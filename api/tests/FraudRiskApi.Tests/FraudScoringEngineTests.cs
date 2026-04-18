using FraudRiskApi.Models;
using FraudRiskApi.Services;

namespace FraudRiskApi.Tests;

public sealed class FraudScoringEngineTests
{
    private readonly FraudScoringEngine _engine = new();

    [Fact]
    public void CalculateScore_AllRulesTriggered_ReturnsSumOfWeights()
    {
        var request = new TransactionRequest
        {
            Amount = 10_001m,
            IsNewLocation = true,
            TransactionsInLastHour = 6
        };

        var result = _engine.CalculateScore(request);

        Assert.Equal(100, result.Score);
        Assert.Equal(3, result.AppliedRules.Count);
    }

    [Fact]
    public void CalculateScore_NoRules_ReturnsZero()
    {
        var request = new TransactionRequest
        {
            Amount = 100m,
            IsNewLocation = false,
            TransactionsInLastHour = 2
        };

        var result = _engine.CalculateScore(request);

        Assert.Equal(0, result.Score);
        Assert.Empty(result.AppliedRules);
    }

    [Fact]
    public void CalculateScore_HighVelocityAtThreshold_DoesNotAddVelocity()
    {
        var request = new TransactionRequest
        {
            Amount = 0m,
            IsNewLocation = false,
            TransactionsInLastHour = 5
        };

        var result = _engine.CalculateScore(request);

        Assert.Equal(0, result.Score);
    }
}
