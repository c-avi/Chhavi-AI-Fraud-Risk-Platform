namespace FraudRiskApi.Models;

public sealed class TransactionAlertResponse
{
    public string UserId { get; init; } = string.Empty;
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = "Low";
    public DateTime Timestamp { get; init; }
}
