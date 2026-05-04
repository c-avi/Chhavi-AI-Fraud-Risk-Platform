namespace FraudRiskApi.Models;

public enum BeginScoringKind
{
    CreatedPending,
    AlreadyPending,
    CompletedReplay,
    Conflict
}

public sealed class BeginScoringResult
{
    public required BeginScoringKind Kind { get; init; }
    public int TransactionId { get; init; }
    public RiskScoreResponse? CompletedScore { get; init; }
}
