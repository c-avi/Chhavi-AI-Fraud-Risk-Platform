using FraudRiskApi.Controllers;
using FraudRiskApi.Models;
using FraudRiskApi.Repositories;
using FraudRiskApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudRiskApi.Tests;

public sealed class AiControllerIntegrationTests
{
    [Fact]
    public async Task Predict_ValidPayload_ReturnsPrediction()
    {
        var controller = CreateController();

        var actionResult = await controller.Predict(new AiPredictionRequest
        {
            UserId = "user-api-100",
            Amount = 12_500m,
            Location = "Bengaluru",
            Timestamp = new DateTime(2026, 4, 30, 12, 0, 0, DateTimeKind.Utc)
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var prediction = Assert.IsType<AiPredictionResponse>(okResult.Value);
        Assert.Equal("user-api-100", prediction.UserId);
        Assert.Equal(76, prediction.RiskScore);
        Assert.Equal("High", prediction.RiskLevel);
        Assert.Equal(0.76m, prediction.ConfidenceScore);
    }

    private static AiController CreateController()
    {
        var predictionService = new AiPredictionService(
            new FixedFraudRiskModelEngine(),
            new InMemoryTransactionRepository(),
            NullLogger<AiPredictionService>.Instance);

        return new AiController(
            predictionService,
            NullLogger<AiController>.Instance);
    }

    private sealed class FixedFraudRiskModelEngine : IFraudRiskModelEngine
    {
        public ValueTask<FraudRiskAssessment> EvaluateAsync(FraudRiskContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new FraudRiskAssessment
            {
                RiskScore = 76,
                RiskLevel = "High"
            });
        }
    }
}

