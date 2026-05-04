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

    public FraudFeatureSet GetAuditFeatureSet(FraudRiskContext context)
    {
        var amountScale = Math.Max((float)context.HistoricalAverageAmount * 4f, 20_000f);
        var normalizedAmount = NormalizeAmount((float)context.Amount, amountScale);
        var amountToAverageRatio = GetAmountRatio((float)context.Amount, (float)context.HistoricalAverageAmount);
        var recentCount = Math.Max(0, context.RecentTransactionCount);
        var distinctLocations = Math.Max(0, context.DistinctLocationCount);
        var locationChanged = context.LastTransaction is not null &&
            !string.Equals(
                context.LastTransaction.Location,
                context.Location,
                StringComparison.OrdinalIgnoreCase);

        return new FraudFeatureSet
        {
            NormalizedAmount = normalizedAmount,
            AmountToAverageRatio = amountToAverageRatio,
            RecentTransactionCount = recentCount,
            DistinctLocationCount = distinctLocations,
            LocationChangedSinceLast = locationChanged,
            GeoVelocity = new GeoVelocitySnapshot
            {
                DistinctLocationsInWindow = distinctLocations,
                LocationChangedSinceLastTransaction = locationChanged
            }
        };
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

    private static float NormalizeAmount(float amount, float scale)
    {
        if (scale <= 0f)
        {
            return 0f;
        }

        var normalized = amount / scale;
        return Math.Clamp(normalized, 0f, 1f);
    }

    private static float GetAmountRatio(float amount, float historicalAverageAmount)
    {
        if (historicalAverageAmount <= 0f)
        {
            return amount > 0f ? 1f : 0f;
        }

        return MathF.Max(0f, amount / historicalAverageAmount);
    }
}
