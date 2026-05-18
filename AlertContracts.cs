using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ZavaAlertService
{
    [DataContract]
    public class AlertRuleDefinition
    {
        [DataMember] public int AccountId { get; set; }
        [DataMember] public int CustomerId { get; set; }
        [DataMember] public string RuleName { get; set; }
        [DataMember] public string RuleCode { get; set; }
        [DataMember] public string Threshold { get; set; }
        [DataMember] public string Category { get; set; }
        [DataMember] public bool IsActive { get; set; }
        [DataMember] public string NotificationChannel { get; set; }
    }

    [DataContract]
    public class AlertRuleInfo
    {
        [DataMember] public int RuleId { get; set; }
        [DataMember] public int AccountAlertId { get; set; }
        [DataMember] public int AccountId { get; set; }
        [DataMember] public string RuleName { get; set; }
        [DataMember] public string RuleCode { get; set; }
        [DataMember] public string Threshold { get; set; }
        [DataMember] public string Category { get; set; }
        [DataMember] public bool IsEnabled { get; set; }
        [DataMember] public string NotificationChannel { get; set; }
    }

    [DataContract]
    public class AlertRulesResponse
    {
        [DataMember] public int AccountId { get; set; }
        [DataMember] public List<AlertRuleInfo> Rules { get; set; }
    }

    [DataContract]
    public class AlertEvaluationResponse
    {
        [DataMember] public long TransactionId { get; set; }
        [DataMember] public int AccountId { get; set; }
        [DataMember] public bool IsAlertTriggered { get; set; }
        [DataMember] public List<string> TriggeredRuleCodes { get; set; }
        [DataMember] public bool PublishedToNotificationsQueue { get; set; }
        [DataMember] public string Message { get; set; }
    }

    [DataContract]
    public class AlertRuleMutationResponse
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public int RuleId { get; set; }
        [DataMember] public int AccountAlertId { get; set; }
        [DataMember] public string Message { get; set; }
    }

    internal sealed class EvaluatedRule
    {
        public int AccountAlertId { get; set; }
        public int RuleId { get; set; }
        public string RuleCode { get; set; }
        public string RuleName { get; set; }
        public string Threshold { get; set; }
        public string NotificationChannel { get; set; }
    }

    internal sealed class AlertEvent
    {
        public long TransactionId { get; set; }
        public int AccountId { get; set; }
        public int RuleId { get; set; }
        public string RuleCode { get; set; }
        public string RuleName { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime TriggeredAtUtc { get; set; }
    }
}
