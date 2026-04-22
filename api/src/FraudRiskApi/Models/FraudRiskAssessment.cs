namespace FraudRiskApi.Models;

public sealed class FraudRiskAssessment
{
    public required int RiskScore { get; init; }
    public required string RiskLevel { get; init; }
}
