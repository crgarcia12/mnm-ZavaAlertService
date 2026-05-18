using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.ServiceModel;

namespace ZavaAlertService
{
    public class AlertService : IAlertService
    {
        private readonly RabbitMqPublisher _publisher = new RabbitMqPublisher();

        public AlertEvaluationResponse EvaluateTransaction(long transactionId)
        {
            if (transactionId <= 0)
            {
                throw new FaultException("transactionId must be greater than 0.");
            }

            using (var connection = new SqlConnection(DbConfig.GetConnectionString()))
            {
                connection.Open();

                int accountId;
                decimal amount;
                decimal balanceAfter;
                int customerId;
                string channel;

                using (var txnCommand = new SqlCommand(@"
SELECT TOP 1 t.AccountID, t.Amount, t.BalanceAfter, a.CustomerID, ISNULL(t.Channel, '')
FROM Transactions t
INNER JOIN Accounts a ON a.AccountID = t.AccountID
WHERE t.TransactionID = @TransactionID;", connection))
                {
                    txnCommand.Parameters.AddWithValue("@TransactionID", transactionId);
                    using (var reader = txnCommand.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new FaultException("Transaction was not found.");
                        }

                        accountId = reader.GetInt32(0);
                        amount = reader.GetDecimal(1);
                        balanceAfter = reader.GetDecimal(2);
                        customerId = reader.GetInt32(3);
                        channel = reader.GetString(4);
                    }
                }

                var triggeredCodes = new List<string>();
                var rules = LoadAccountRules(connection, accountId);
                var publishedAny = false;

                foreach (var rule in rules)
                {
                    if (!IsTriggered(rule.RuleCode, rule.Threshold, amount, balanceAfter, accountId, transactionId, channel, connection))
                    {
                        continue;
                    }

                    triggeredCodes.Add(rule.RuleCode);
                    var alertEvent = new AlertEvent
                    {
                        TransactionId = transactionId,
                        AccountId = accountId,
                        RuleId = rule.RuleId,
                        RuleCode = rule.RuleCode,
                        RuleName = rule.RuleName,
                        Amount = amount,
                        BalanceAfter = balanceAfter,
                        TriggeredAtUtc = DateTime.UtcNow
                    };

                    var published = _publisher.PublishAlert(alertEvent);
                    publishedAny = publishedAny || published;

                    using (var historyCommand = new SqlCommand(@"
INSERT INTO AlertHistory (AccountAlertID, TriggeredDate, TriggerValue, NotificationSent, NotificationDate, Details)
VALUES (@AccountAlertID, GETDATE(), @TriggerValue, @NotificationSent, CASE WHEN @NotificationSent = 1 THEN GETDATE() ELSE NULL END, @Details);", connection))
                    {
                        historyCommand.Parameters.AddWithValue("@AccountAlertID", rule.AccountAlertId);
                        historyCommand.Parameters.AddWithValue("@TriggerValue", amount.ToString("F2", CultureInfo.InvariantCulture));
                        historyCommand.Parameters.AddWithValue("@NotificationSent", published ? 1 : 0);
                        historyCommand.Parameters.AddWithValue("@Details", string.Format(
                            CultureInfo.InvariantCulture,
                            "Rule {0} triggered for account {1}. BalanceAfter={2:F2}. Channel={3}",
                            rule.RuleCode,
                            accountId,
                            balanceAfter,
                            channel));
                        historyCommand.ExecuteNonQuery();
                    }
                }

                return new AlertEvaluationResponse
                {
                    TransactionId = transactionId,
                    AccountId = accountId,
                    IsAlertTriggered = triggeredCodes.Count > 0,
                    TriggeredRuleCodes = triggeredCodes,
                    PublishedToNotificationsQueue = publishedAny,
                    Message = triggeredCodes.Count == 0
                        ? "No active alert rules were triggered."
                        : string.Format(CultureInfo.InvariantCulture, "Triggered {0} alert rule(s) for customer {1}.", triggeredCodes.Count, customerId)
                };
            }
        }

        public AlertRulesResponse GetAlertRules(int accountId)
        {
            if (accountId <= 0)
            {
                throw new FaultException("accountId must be greater than 0.");
            }

            var response = new AlertRulesResponse
            {
                AccountId = accountId,
                Rules = new List<AlertRuleInfo>()
            };

            using (var connection = new SqlConnection(DbConfig.GetConnectionString()))
            using (var command = new SqlCommand(@"
SELECT aa.AccountAlertID, ar.RuleID, aa.AccountID, ar.RuleName, ar.RuleCode,
       ISNULL(NULLIF(aa.Threshold, ''), ar.DefaultThreshold) AS Threshold,
       ar.Category, aa.IsEnabled, aa.NotificationChannel
FROM AccountAlerts aa
INNER JOIN AlertRules ar ON ar.RuleID = aa.RuleID
WHERE aa.AccountID = @AccountID
ORDER BY ar.RuleName;", connection))
            {
                command.Parameters.AddWithValue("@AccountID", accountId);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        response.Rules.Add(new AlertRuleInfo
                        {
                            AccountAlertId = reader.GetInt32(0),
                            RuleId = reader.GetInt32(1),
                            AccountId = reader.GetInt32(2),
                            RuleName = reader.GetString(3),
                            RuleCode = reader.GetString(4),
                            Threshold = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Category = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            IsEnabled = reader.GetBoolean(7),
                            NotificationChannel = reader.IsDBNull(8) ? "Email" : reader.GetString(8)
                        });
                    }
                }
            }

            return response;
        }

        public AlertRuleMutationResponse CreateAlertRule(AlertRuleDefinition rule)
        {
            ValidateRule(rule, requireAccount: true);

            using (var connection = new SqlConnection(DbConfig.GetConnectionString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    int ruleId;
                    using (var insertRule = new SqlCommand(@"
INSERT INTO AlertRules (RuleName, RuleCode, DefaultThreshold, Category, IsActive, CreatedDate)
VALUES (@RuleName, @RuleCode, @DefaultThreshold, @Category, @IsActive, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                    {
                        insertRule.Parameters.AddWithValue("@RuleName", rule.RuleName.Trim());
                        insertRule.Parameters.AddWithValue("@RuleCode", rule.RuleCode.Trim().ToUpperInvariant());
                        insertRule.Parameters.AddWithValue("@DefaultThreshold", string.IsNullOrWhiteSpace(rule.Threshold) ? "0" : rule.Threshold.Trim());
                        insertRule.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(rule.Category) ? "Transaction" : rule.Category.Trim());
                        insertRule.Parameters.AddWithValue("@IsActive", rule.IsActive ? 1 : 0);
                        ruleId = (int)insertRule.ExecuteScalar();
                    }

                    int accountAlertId;
                    using (var insertAccountAlert = new SqlCommand(@"
INSERT INTO AccountAlerts (CustomerID, AccountID, RuleID, Threshold, NotificationChannel, IsEnabled, CreatedDate, ModifiedDate)
VALUES (@CustomerID, @AccountID, @RuleID, @Threshold, @NotificationChannel, @IsEnabled, GETDATE(), GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                    {
                        insertAccountAlert.Parameters.AddWithValue("@CustomerID", rule.CustomerId);
                        insertAccountAlert.Parameters.AddWithValue("@AccountID", rule.AccountId);
                        insertAccountAlert.Parameters.AddWithValue("@RuleID", ruleId);
                        insertAccountAlert.Parameters.AddWithValue("@Threshold", string.IsNullOrWhiteSpace(rule.Threshold) ? "0" : rule.Threshold.Trim());
                        insertAccountAlert.Parameters.AddWithValue("@NotificationChannel", string.IsNullOrWhiteSpace(rule.NotificationChannel) ? "Email" : rule.NotificationChannel.Trim());
                        insertAccountAlert.Parameters.AddWithValue("@IsEnabled", rule.IsActive ? 1 : 0);
                        accountAlertId = (int)insertAccountAlert.ExecuteScalar();
                    }

                    transaction.Commit();

                    return new AlertRuleMutationResponse
                    {
                        Success = true,
                        RuleId = ruleId,
                        AccountAlertId = accountAlertId,
                        Message = "Alert rule created."
                    };
                }
            }
        }

        public AlertRuleMutationResponse UpdateAlertRule(int ruleId, AlertRuleDefinition rule)
        {
            if (ruleId <= 0)
            {
                throw new FaultException("ruleId must be greater than 0.");
            }

            ValidateRule(rule, requireAccount: false);

            using (var connection = new SqlConnection(DbConfig.GetConnectionString()))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var updateRule = new SqlCommand(@"
UPDATE AlertRules
SET RuleName = @RuleName,
    RuleCode = @RuleCode,
    DefaultThreshold = @DefaultThreshold,
    Category = @Category,
    IsActive = @IsActive
WHERE RuleID = @RuleID;", connection, transaction))
                    {
                        updateRule.Parameters.AddWithValue("@RuleName", rule.RuleName.Trim());
                        updateRule.Parameters.AddWithValue("@RuleCode", rule.RuleCode.Trim().ToUpperInvariant());
                        updateRule.Parameters.AddWithValue("@DefaultThreshold", string.IsNullOrWhiteSpace(rule.Threshold) ? "0" : rule.Threshold.Trim());
                        updateRule.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(rule.Category) ? "Transaction" : rule.Category.Trim());
                        updateRule.Parameters.AddWithValue("@IsActive", rule.IsActive ? 1 : 0);
                        updateRule.Parameters.AddWithValue("@RuleID", ruleId);

                        if (updateRule.ExecuteNonQuery() == 0)
                        {
                            throw new FaultException("Alert rule was not found.");
                        }
                    }

                    int accountAlertId = 0;
                    if (rule.AccountId > 0)
                    {
                        using (var updateAccountAlert = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM AccountAlerts WHERE RuleID = @RuleID AND AccountID = @AccountID)
BEGIN
    UPDATE AccountAlerts
    SET Threshold = @Threshold,
        NotificationChannel = @NotificationChannel,
        IsEnabled = @IsEnabled,
        ModifiedDate = GETDATE()
    WHERE RuleID = @RuleID AND AccountID = @AccountID;
END
ELSE
BEGIN
    INSERT INTO AccountAlerts (CustomerID, AccountID, RuleID, Threshold, NotificationChannel, IsEnabled, CreatedDate, ModifiedDate)
    VALUES (@CustomerID, @AccountID, @RuleID, @Threshold, @NotificationChannel, @IsEnabled, GETDATE(), GETDATE());
END
SELECT TOP 1 AccountAlertID FROM AccountAlerts WHERE RuleID = @RuleID AND AccountID = @AccountID ORDER BY AccountAlertID DESC;", connection, transaction))
                        {
                            updateAccountAlert.Parameters.AddWithValue("@CustomerID", rule.CustomerId);
                            updateAccountAlert.Parameters.AddWithValue("@AccountID", rule.AccountId);
                            updateAccountAlert.Parameters.AddWithValue("@RuleID", ruleId);
                            updateAccountAlert.Parameters.AddWithValue("@Threshold", string.IsNullOrWhiteSpace(rule.Threshold) ? "0" : rule.Threshold.Trim());
                            updateAccountAlert.Parameters.AddWithValue("@NotificationChannel", string.IsNullOrWhiteSpace(rule.NotificationChannel) ? "Email" : rule.NotificationChannel.Trim());
                            updateAccountAlert.Parameters.AddWithValue("@IsEnabled", rule.IsActive ? 1 : 0);
                            var result = updateAccountAlert.ExecuteScalar();
                            if (result != null)
                            {
                                accountAlertId = Convert.ToInt32(result, CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    transaction.Commit();

                    return new AlertRuleMutationResponse
                    {
                        Success = true,
                        RuleId = ruleId,
                        AccountAlertId = accountAlertId,
                        Message = "Alert rule updated."
                    };
                }
            }
        }

        private static List<EvaluatedRule> LoadAccountRules(SqlConnection connection, int accountId)
        {
            var rules = new List<EvaluatedRule>();
            using (var command = new SqlCommand(@"
SELECT aa.AccountAlertID, ar.RuleID, ar.RuleCode, ar.RuleName,
       ISNULL(NULLIF(aa.Threshold, ''), ar.DefaultThreshold) AS Threshold,
       aa.NotificationChannel
FROM AccountAlerts aa
INNER JOIN AlertRules ar ON ar.RuleID = aa.RuleID
WHERE aa.AccountID = @AccountID
  AND aa.IsEnabled = 1
  AND ar.IsActive = 1;", connection))
            {
                command.Parameters.AddWithValue("@AccountID", accountId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rules.Add(new EvaluatedRule
                        {
                            AccountAlertId = reader.GetInt32(0),
                            RuleId = reader.GetInt32(1),
                            RuleCode = reader.GetString(2),
                            RuleName = reader.GetString(3),
                            Threshold = reader.IsDBNull(4) ? "0" : reader.GetString(4),
                            NotificationChannel = reader.IsDBNull(5) ? "Email" : reader.GetString(5)
                        });
                    }
                }
            }

            return rules;
        }

        private static bool IsTriggered(
            string ruleCode,
            string thresholdValue,
            decimal amount,
            decimal balanceAfter,
            int accountId,
            long transactionId,
            string channel,
            SqlConnection connection)
        {
            decimal threshold;
            if (!decimal.TryParse(thresholdValue, NumberStyles.Any, CultureInfo.InvariantCulture, out threshold))
            {
                threshold = 0m;
            }

            switch ((ruleCode ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "LOW_BAL":
                    return balanceAfter < threshold;
                case "LARGE_TXN":
                    return Math.Abs(amount) >= threshold;
                case "OVERDRAFT":
                    return balanceAfter < 0m;
                case "INTL_WIRE":
                    return channel.IndexOf("WIRE", StringComparison.OrdinalIgnoreCase) >= 0 && Math.Abs(amount) >= threshold;
                case "DAILY_SPEND":
                    return GetDailySpend(accountId, transactionId, connection) >= threshold;
                default:
                    return false;
            }
        }

        private static decimal GetDailySpend(int accountId, long transactionId, SqlConnection connection)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(CASE WHEN Amount < 0 THEN ABS(Amount) ELSE 0 END), 0)
FROM Transactions
WHERE AccountID = @AccountID
  AND CAST(TransactionDate AS DATE) = CAST((SELECT TransactionDate FROM Transactions WHERE TransactionID = @TransactionID) AS DATE);", connection))
            {
                command.Parameters.AddWithValue("@AccountID", accountId);
                command.Parameters.AddWithValue("@TransactionID", transactionId);
                return Convert.ToDecimal(command.ExecuteScalar() ?? 0m, CultureInfo.InvariantCulture);
            }
        }

        private static void ValidateRule(AlertRuleDefinition rule, bool requireAccount)
        {
            if (rule == null)
            {
                throw new FaultException("rule is required.");
            }

            if (string.IsNullOrWhiteSpace(rule.RuleName) || string.IsNullOrWhiteSpace(rule.RuleCode))
            {
                throw new FaultException("RuleName and RuleCode are required.");
            }

            if (requireAccount && (rule.AccountId <= 0 || rule.CustomerId <= 0))
            {
                throw new FaultException("AccountId and CustomerId are required.");
            }
        }
    }
}
