namespace FraudRiskApi.Models;

public sealed class RiskSummaryResponse
{
    public int TotalTransactionsProcessed { get; init; }
    public int HighRiskAlertsCount { get; init; }
    public decimal AverageFraudRiskScore { get; init; }
}
