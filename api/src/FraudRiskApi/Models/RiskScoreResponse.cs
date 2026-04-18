namespace FraudRiskApi.Models;

public sealed class RiskScoreResponse
{
    public int Score { get; init; }

    public IReadOnlyList<string> AppliedRules { get; init; } = Array.Empty<string>();
}
