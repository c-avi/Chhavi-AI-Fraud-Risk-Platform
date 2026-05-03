using FraudRiskApi.Models;
using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class AiPredictionService : IAiPredictionService
{
    private const string ModelVersion = "fraud-risk-ml-v1";
    private readonly IFraudRiskModelEngine _fraudRiskModelEngine;
    private readonly ILogger<AiPredictionService> _logger;
    private readonly ITransactionRepository _transactionRepository;

    public AiPredictionService(
        IFraudRiskModelEngine fraudRiskModelEngine,
        ITransactionRepository transactionRepository,
        ILogger<AiPredictionService> logger)
    {
        _fraudRiskModelEngine = fraudRiskModelEngine;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<AiPredictionResponse> PredictAsync(AiPredictionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        _logger.LogInformation("Starting AI fraud prediction for user {UserId} at {Timestamp}.", request.UserId, request.Timestamp);

        try
        {
            var context = await BuildRiskContextAsync(request, cancellationToken);
            var assessment = await _fraudRiskModelEngine.EvaluateAsync(context, cancellationToken);
            var confidenceScore = ConvertRiskScoreToConfidence(assessment.RiskScore);

            _logger.LogInformation(
                "Completed AI fraud prediction for user {UserId}. RiskScore={RiskScore}, RiskLevel={RiskLevel}, Confidence={ConfidenceScore}.",
                request.UserId,
                assessment.RiskScore,
                assessment.RiskLevel,
                confidenceScore);

            return new AiPredictionResponse
            {
                UserId = request.UserId,
                RiskScore = assessment.RiskScore,
                RiskLevel = assessment.RiskLevel,
                ConfidenceScore = confidenceScore,
                ModelVersion = ModelVersion,
                PredictedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (ex is not ArgumentException and not OperationCanceledException)
        {
            _logger.LogError(ex, "AI fraud prediction failed for user {UserId}.", request.UserId);
            throw;
        }
    }

    private async Task<FraudRiskContext> BuildRiskContextAsync(AiPredictionRequest request, CancellationToken cancellationToken)
    {
        var recentWindowStart = request.Timestamp.AddMinutes(-15);
        var historyWindowStart = request.Timestamp.AddDays(-30);

        var lastTransaction = await _transactionRepository.GetLastTransactionAsync(request.UserId, cancellationToken);
        var recentTransactionCount = await _transactionRepository.CountTransactionsSinceAsync(
            request.UserId,
            recentWindowStart,
            request.Timestamp,
            cancellationToken);
        var historicalAverageAmount = await _transactionRepository.GetAverageAmountAsync(
            request.UserId,
            historyWindowStart,
            request.Timestamp,
            cancellationToken);
        var distinctLocationCount = await _transactionRepository.CountDistinctLocationsSinceAsync(
            request.UserId,
            historyWindowStart,
            request.Timestamp,
            cancellationToken);

        return new FraudRiskContext
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Location = request.Location,
            Timestamp = request.Timestamp,
            LastTransaction = lastTransaction,
            RecentTransactionCount = recentTransactionCount,
            HistoricalAverageAmount = historicalAverageAmount,
            DistinctLocationCount = distinctLocationCount
        };
    }

    private static decimal ConvertRiskScoreToConfidence(int riskScore)
    {
        if (riskScore < 0)
        {
            return 0m;
        }

        return decimal.Round(Math.Clamp(riskScore, 0, 100) / 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateRequest(AiPredictionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            throw new ArgumentException("Location is required.");
        }

        if (request.Timestamp == default)
        {
            throw new ArgumentException("Timestamp is required.");
        }
    }
}

