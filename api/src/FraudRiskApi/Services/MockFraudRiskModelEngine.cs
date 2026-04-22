using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public sealed class MockFraudRiskModelEngine : IFraudRiskModelEngine
{
    private const decimal ElevatedAmountThreshold = 10_000m;

    public ValueTask<FraudRiskAssessment> EvaluateAsync(FraudRiskContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var frequencyRisk = CalculateFrequencyRisk(context.RecentTransactionCount);
        var locationRisk = CalculateLocationRisk(context);
        var amountRisk = CalculateAmountRisk(context.Amount, context.HistoricalAverageAmount);
        var score = Math.Clamp(frequencyRisk + locationRisk + amountRisk, 0, 100);

        return ValueTask.FromResult(new FraudRiskAssessment
        {
            RiskScore = score,
            RiskLevel = GetRiskLevel(score)
        });
    }

    private static int CalculateFrequencyRisk(int recentTransactionCount) =>
        recentTransactionCount switch
        {
            >= 4 => 40,
            >= 2 => 28,
            >= 1 => 16,
            _ => 0
        };

    private static int CalculateLocationRisk(FraudRiskContext context)
    {
        if (context.LastTransaction is null)
        {
            return 0;
        }

        var changedLocation = !string.Equals(
            context.LastTransaction.Location,
            context.Location,
            StringComparison.OrdinalIgnoreCase);

        if (!changedLocation)
        {
            return 0;
        }

        return context.DistinctLocationCount switch
        {
            >= 4 => 34,
            >= 2 => 28,
            _ => 22
        };
    }

    private static int CalculateAmountRisk(decimal amount, decimal historicalAverageAmount)
    {
        if (historicalAverageAmount <= 0)
        {
            return amount >= ElevatedAmountThreshold ? 18 : 0;
        }

        var ratio = amount / historicalAverageAmount;

        if (ratio >= 3.5m)
        {
            return 34;
        }

        if (ratio >= 2.2m)
        {
            return 24;
        }

        if (ratio >= 1.5m || amount >= ElevatedAmountThreshold)
        {
            return 14;
        }

        return 0;
    }

    private static string GetRiskLevel(int score) =>
        score switch
        {
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };
}
