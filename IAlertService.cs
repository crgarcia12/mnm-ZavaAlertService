using System.ServiceModel;

namespace ZavaAlertService
{
    [ServiceContract]
    public interface IAlertService
    {
        [OperationContract]
        AlertEvaluationResponse EvaluateTransaction(long transactionId);

        [OperationContract]
        AlertRulesResponse GetAlertRules(int accountId);

        [OperationContract]
        AlertRuleMutationResponse CreateAlertRule(AlertRuleDefinition rule);

        [OperationContract]
        AlertRuleMutationResponse UpdateAlertRule(int ruleId, AlertRuleDefinition rule);
    }
}
