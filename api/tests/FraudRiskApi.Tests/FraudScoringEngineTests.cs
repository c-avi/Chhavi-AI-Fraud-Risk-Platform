using FraudRiskApi.Models;
using FraudRiskApi.Repositories;
using FraudRiskApi.Services;

namespace FraudRiskApi.Tests;

public sealed class FraudScoringServiceTests
{
    [Fact]
    public async Task ScoreTransactionAsync_LocationVelocityAndAmountDeviation_ReturnsHighRisk()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);

        await SubmitAndCompleteAsync(service, repository, new TransactionRequest
        {
            UserId = "user-100",
            Amount = 500m,
            Location = "Mumbai",
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        });

        await SubmitAndCompleteAsync(service, repository, new TransactionRequest
        {
            UserId = "user-100",
            Amount = 650m,
            Location = "Mumbai",
            Timestamp = DateTime.UtcNow.AddMinutes(-8)
        });

        await SubmitAndCompleteAsync(service, repository, new TransactionRequest
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

        var result = await SubmitAndCompleteAsync(service, repository, request);

        Assert.InRange(result.RiskScore, 70, 100);
        Assert.Equal("High", result.RiskLevel);
    }

    [Fact]
    public async Task ScoreTransactionAsync_NoRules_ReturnsZero()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);

        var request = new TransactionRequest
        {
            UserId = "user-200",
            Amount = 100m,
            Location = "Pune",
            Timestamp = DateTime.UtcNow
        };

        var result = await SubmitAndCompleteAsync(service, repository, request);

        Assert.Equal(0, result.RiskScore);
        Assert.Equal("Low", result.RiskLevel);
    }

    [Fact]
    public async Task GetRiskSummaryAsync_ReturnsAggregatedMetrics()
    {
        var repository = new InMemoryTransactionRepository();
        var service = CreateService(repository);

        await SubmitAndCompleteAsync(service, repository, new TransactionRequest
        {
            UserId = "user-300",
            Amount = 15_000m,
            Location = "Bengaluru",
            Timestamp = DateTime.UtcNow.AddMinutes(-2)
        });

        await SubmitAndCompleteAsync(service, repository, new TransactionRequest
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
    public async Task ScoreTransactionAsync_HighRiskScore_PersistsAlertWithFeatureJson()
    {
        var alertRepository = new InMemoryAlertRepository();
        var transactionRepository = new InMemoryTransactionRepository();
        var service = CreateService(transactionRepository, alertRepository);

        await SubmitAndCompleteAsync(service, transactionRepository, new TransactionRequest
        {
            UserId = "user-410",
            Amount = 200m,
            Location = "Delhi",
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        });

        await SubmitAndCompleteAsync(service, transactionRepository, new TransactionRequest
        {
            UserId = "user-410",
            Amount = 650m,
            Location = "Delhi",
            Timestamp = DateTime.UtcNow.AddMinutes(-8)
        });

        await SubmitAndCompleteAsync(service, transactionRepository, new TransactionRequest
        {
            UserId = "user-410",
            Amount = 800m,
            Location = "Delhi",
            Timestamp = DateTime.UtcNow.AddMinutes(-6)
        });

        var result = await SubmitAndCompleteAsync(service, transactionRepository, new TransactionRequest
        {
            UserId = "user-410",
            Amount = 12_000m,
            Location = "Kolkata",
            Timestamp = DateTime.UtcNow
        });

        var alerts = await alertRepository.GetRecentAsync(10);

        var alert = Assert.Single(alerts);
        Assert.Equal(result.TransactionId, alert.TransactionId);
        Assert.InRange(alert.RiskScore, 70, 100);
        Assert.False(string.IsNullOrWhiteSpace(alert.FeatureSetJson));
        Assert.Contains("amountToAverageRatio", alert.FeatureSetJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("geoVelocity", alert.FeatureSetJson, StringComparison.OrdinalIgnoreCase);
    }

    private static FraudScoringService CreateService(
        InMemoryTransactionRepository transactionRepository,
        IAlertRepository? alertRepository = null)
    {
        alertRepository ??= new InMemoryAlertRepository();

        return new FraudScoringService(
            transactionRepository,
            new MockFraudRiskModelEngine(),
            new AlertService(alertRepository),
            new InMemoryScoringSubmissionStore(transactionRepository),
            new FraudScoringQueue());
    }

    private static async Task<RiskScoreResponse> SubmitAndCompleteAsync(
        FraudScoringService service,
        InMemoryTransactionRepository repository,
        TransactionRequest request)
    {
        var begin = await service.SubmitTransactionForScoringAsync(
            request,
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        await service.ProcessPendingTransactionAsync(begin.TransactionId, CancellationToken.None);

        var tx = await repository.GetByIdAsync(begin.TransactionId, CancellationToken.None);
        Assert.NotNull(tx);
        return new RiskScoreResponse
        {
            TransactionId = tx!.TransactionId,
            RiskScore = tx.RiskScore,
            RiskLevel = tx.RiskLevel
        };
    }
}
