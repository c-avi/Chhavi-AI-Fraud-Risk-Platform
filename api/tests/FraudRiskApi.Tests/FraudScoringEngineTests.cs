using FraudRiskApi.Models;
using FraudRiskApi.Repositories;
using FraudRiskApi.Services;

namespace FraudRiskApi.Tests;

public sealed class FraudScoringServiceTests
{
    [Fact]
    public async Task ScoreTransactionAsync_LocationVelocityAndAmountDeviation_ReturnsHighRisk()
    {
        var service = CreateService();

        await service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-100",
            Amount = 500m,
            Location = "Mumbai",
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        });

        await service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-100",
            Amount = 650m,
            Location = "Mumbai",
            Timestamp = DateTime.UtcNow.AddMinutes(-8)
        });

        await service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-100",
            Amount = 800m,
            Location = "Mumbai",
            Timestamp = DateTime.UtcNow.AddMinutes(-6)
        });

        var request = new TransactionRequest
        {
            UserId = "user-100",
            Amount = 12_000m,
            Location = "Delhi",
            Timestamp = DateTime.UtcNow
        };

        var result = await service.ScoreTransactionAsync(request);

        Assert.InRange(result.RiskScore, 70, 100);
        Assert.Equal("High", result.RiskLevel);
    }

    [Fact]
    public async Task ScoreTransactionAsync_NoRules_ReturnsZero()
    {
        var service = CreateService();

        var request = new TransactionRequest
        {
            UserId = "user-200",
            Amount = 100m,
            Location = "Pune",
            Timestamp = DateTime.UtcNow
        };

        var result = await service.ScoreTransactionAsync(request);

        Assert.Equal(0, result.RiskScore);
        Assert.Equal("Low", result.RiskLevel);
    }

    [Fact]
    public async Task GetRiskSummaryAsync_ReturnsAggregatedMetrics()
    {
        var service = CreateService();

        await service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-300",
            Amount = 15_000m,
            Location = "Bengaluru",
            Timestamp = DateTime.UtcNow.AddMinutes(-2)
        });

        await service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-301",
            Amount = 300m,
            Location = "Bengaluru",
            Timestamp = DateTime.UtcNow
        });

        var summary = await service.GetRiskSummaryAsync();

        Assert.Equal(2, summary.TotalTransactionsProcessed);
        Assert.Equal(0, summary.HighRiskAlertsCount);
        Assert.True(summary.AverageFraudRiskScore >= 0);
    }

    [Fact]
    public async Task GetRecentAlertsAsync_ReturnsMostRecentTransactionsFirst()
    {
        var service = CreateService();

        await service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-410",
            Amount = 200m,
            Location = "Delhi",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });

        await service.ScoreTransactionAsync(new TransactionRequest
        {
            UserId = "user-410",
            Amount = 500m,
            Location = "Delhi",
            Timestamp = DateTime.UtcNow
        });

        var alerts = await service.GetRecentAlertsAsync(2);

        Assert.Equal(2, alerts.Count);
        Assert.True(alerts[0].Timestamp >= alerts[1].Timestamp);
    }

    private static FraudScoringService CreateService()
    {
        return new FraudScoringService(
            new InMemoryTransactionRepository(),
            new MockFraudRiskModelEngine());
    }
}
