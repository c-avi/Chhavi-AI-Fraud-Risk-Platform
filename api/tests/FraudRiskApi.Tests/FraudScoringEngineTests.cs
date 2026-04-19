using FraudRiskApi.Models;
using FraudRiskApi.Repositories;
using FraudRiskApi.Services;

namespace FraudRiskApi.Tests;

public sealed class FraudScoringServiceTests
{
    private readonly FraudScoringService _service = new(new InMemoryTransactionRepository());

    [Fact]
    public async Task ScoreTransactionAsync_AllRulesTriggered_ReturnsSumOfWeights()
    {
        await _service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-100",
            Amount = 500m,
            Location = "Mumbai",
            Timestamp = DateTime.UtcNow.AddSeconds(-20)
        });

        var request = new TransactionRequest
        {
            UserId = "user-100",
            Amount = 10_001m,
            Location = "Delhi",
            Timestamp = DateTime.UtcNow
        };

        var result = await _service.ScoreTransactionAsync(request);

        Assert.Equal(100, result.RiskScore);
        Assert.Equal("High", result.RiskLevel);
    }

    [Fact]
    public async Task ScoreTransactionAsync_NoRules_ReturnsZero()
    {
        var request = new TransactionRequest
        {
            UserId = "user-200",
            Amount = 100m,
            Location = "Pune",
            Timestamp = DateTime.UtcNow
        };

        var result = await _service.ScoreTransactionAsync(request);

        Assert.Equal(0, result.RiskScore);
        Assert.Equal("Low", result.RiskLevel);
    }

    [Fact]
    public async Task ScoreTransactionAsync_OnlyHighAmount_ReturnsLowRisk()
    {
        var request = new TransactionRequest
        {
            UserId = "user-300",
            Amount = 15_000m,
            Location = "Bengaluru",
            Timestamp = DateTime.UtcNow
        };

        var result = await _service.ScoreTransactionAsync(request);

        Assert.Equal(30, result.RiskScore);
        Assert.Equal("Low", result.RiskLevel);
    }
}
