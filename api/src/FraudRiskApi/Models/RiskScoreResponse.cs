namespace FraudRiskApi.Models;

public sealed class RiskScoreResponse
{
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = "Low";
}
