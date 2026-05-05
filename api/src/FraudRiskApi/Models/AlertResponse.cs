namespace FraudRiskApi.Models;

public sealed class AlertResponse
{
    public int Id { get; init; }
    public int TransactionId { get; init; }
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = "Low";
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<string> RiskIndicators { get; init; } = Array.Empty<string>();
    public string? FeatureSetJson { get; init; }
}
