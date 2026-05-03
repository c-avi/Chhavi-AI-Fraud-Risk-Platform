namespace FraudRiskApi.Models;

public sealed class AiPredictionResponse
{
    public required string UserId { get; init; }
    public required int RiskScore { get; init; }
    public required string RiskLevel { get; init; }
    public required decimal ConfidenceScore { get; init; }
    public required string ModelVersion { get; init; }
    public required DateTime PredictedAtUtc { get; init; }
}

