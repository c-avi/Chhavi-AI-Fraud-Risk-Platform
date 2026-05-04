using FraudRiskApi.Models;
using FraudRiskApi.Repositories;
using FraudRiskApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudRiskApi.Tests;

public sealed class AiPredictionServiceTests
{
    [Fact]
    public async Task PredictAsync_ValidRequest_ReturnsPredictionWithConfidence()
    {
        var repository = new InMemoryTransactionRepository();
        await repository.AddAsync(new Transaction
        {
            UserId = "user-ai-100",
            Amount = 500m,
            Location = "Mumbai",
            Timestamp = new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc),
            RiskScore = 12,
            RiskLevel = "Low"
        });
        var service = new AiPredictionService(
            new FixedFraudRiskModelEngine(83, "High"),
            repository,
            NullLogger<AiPredictionService>.Instance);

        var result = await service.PredictAsync(new AiPredictionRequest
        {
            UserId = "user-ai-100",
            Amount = 15_000m,
            Location = "Delhi",
            Timestamp = new DateTime(2026, 4, 30, 10, 10, 0, DateTimeKind.Utc)
        });

        Assert.Equal("user-ai-100", result.UserId);
        Assert.Equal(83, result.RiskScore);
        Assert.Equal("High", result.RiskLevel);
        Assert.Equal(0.83m, result.ConfidenceScore);
        Assert.Equal("fraud-risk-ml-v1", result.ModelVersion);
    }

    [Fact]
    public async Task PredictAsync_InvalidAmount_ThrowsArgumentException()
    {
        var service = new AiPredictionService(
            new FixedFraudRiskModelEngine(10, "Low"),
            new InMemoryTransactionRepository(),
            NullLogger<AiPredictionService>.Instance);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.PredictAsync(new AiPredictionRequest
        {
            UserId = "user-ai-200",
            Amount = 0m,
            Location = "Pune",
            Timestamp = DateTime.UtcNow
        }));

        Assert.Equal("Amount must be greater than zero.", exception.Message);
    }

    private sealed class FixedFraudRiskModelEngine : IFraudRiskModelEngine
    {
        private readonly string _riskLevel;
        private readonly int _riskScore;

        public FixedFraudRiskModelEngine(int riskScore, string riskLevel)
        {
            _riskScore = riskScore;
            _riskLevel = riskLevel;
        }

        public ValueTask<FraudRiskAssessment> EvaluateAsync(FraudRiskContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new FraudRiskAssessment
            {
                RiskScore = _riskScore,
                RiskLevel = _riskLevel
            });
        }

        public FraudFeatureSet GetAuditFeatureSet(FraudRiskContext context) =>
            new()
            {
                NormalizedAmount = 0f,
                AmountToAverageRatio = 0f,
                RecentTransactionCount = 0,
                DistinctLocationCount = 0,
                LocationChangedSinceLast = false,
                GeoVelocity = new GeoVelocitySnapshot
                {
                    DistinctLocationsInWindow = 0,
                    LocationChangedSinceLastTransaction = false
                }
            };
    }
}

