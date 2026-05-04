namespace FraudRiskApi.Models;

public enum TransactionScoringStatus : byte
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}
