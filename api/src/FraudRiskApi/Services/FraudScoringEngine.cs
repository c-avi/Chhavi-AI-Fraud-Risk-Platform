using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public sealed class FraudScoringEngine : IFraudScoringEngine
{
    private const decimal HighAmountThreshold = 10_000m;
    private const int AmountWeight = 40;
    private const int NewLocationWeight = 30;
    private const int HighVelocityWeight = 30;
    private const int HighVelocityTransactionThreshold = 5;

    public RiskScoreResponse CalculateScore(TransactionRequest request)
    {
        var rules = new List<string>();
        var score = 0;

        if (request.Amount > HighAmountThreshold)
        {
            score += AmountWeight;
            rules.Add($"Amount exceeds {HighAmountThreshold:N0}: +{AmountWeight}");
        }

        if (request.IsNewLocation)
        {
            score += NewLocationWeight;
            rules.Add($"New location: +{NewLocationWeight}");
        }

        if (request.TransactionsInLastHour > HighVelocityTransactionThreshold)
        {
            score += HighVelocityWeight;
            rules.Add($"High velocity (>{HighVelocityTransactionThreshold} in window): +{HighVelocityWeight}");
        }

        return new RiskScoreResponse
        {
            Score = score,
            AppliedRules = rules
        };
    }
}
