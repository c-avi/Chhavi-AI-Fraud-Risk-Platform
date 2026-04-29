using FraudRiskApi.Models;
using FraudRiskApi.Models.ML;
using Microsoft.ML;

namespace FraudRiskApi.Services;

public sealed class PredictiveFraudModelEngine : IFraudRiskModelEngine
{
    private const float MinimumAmountScale = 20_000f;
    private readonly string _modelArtifactPath;
    private bool _hasModelArtifact;
    private readonly ILogger<PredictiveFraudModelEngine> _logger;
    private readonly PredictionEngine<FraudModelInput, FraudModelOutput>? _predictionEngine;
    private readonly Lock _predictionLock = new();

    public PredictiveFraudModelEngine(IHostEnvironment hostEnvironment, ILogger<PredictiveFraudModelEngine> logger)
    {
        _logger = logger;
        _modelArtifactPath = Path.Combine(hostEnvironment.ContentRootPath, "Models", "ML", "fraud-risk-model.zip");
        _hasModelArtifact = File.Exists(_modelArtifactPath);

        if (!_hasModelArtifact)
        {
            _logger.LogWarning("ML model artifact not found at {ModelArtifactPath}. Falling back to heuristic scoring.", _modelArtifactPath);
            return;
        }

        try
        {
            var mlContext = new MLContext(seed: 42);
            using var modelStream = File.OpenRead(_modelArtifactPath);
            var transformer = mlContext.Model.Load(modelStream, out _);
            _predictionEngine = mlContext.Model.CreatePredictionEngine<FraudModelInput, FraudModelOutput>(transformer);
            _logger.LogInformation("Loaded fraud risk model artifact from {ModelArtifactPath}.", _modelArtifactPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ML model artifact from {ModelArtifactPath}. Falling back to heuristic scoring.", _modelArtifactPath);
            _hasModelArtifact = false;
        }
    }

    public async ValueTask<FraudRiskAssessment> EvaluateAsync(FraudRiskContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var modelInput = BuildModelInput(context);
        var modelOutput = RunInference(modelInput, cancellationToken);

        var riskScore = ConvertProbabilityToRiskScore(modelOutput.Probability);

        return new FraudRiskAssessment
        {
            RiskScore = riskScore,
            RiskLevel = GetRiskLevel(riskScore)
        };
    }

    private FraudModelInput BuildModelInput(FraudRiskContext context)
    {
        var amountScale = Math.Max((float)context.HistoricalAverageAmount * 4f, MinimumAmountScale);
        var normalizedAmount = NormalizeAmount((float)context.Amount, amountScale);
        var amountToAverageRatio = GetAmountRatio((float)context.Amount, (float)context.HistoricalAverageAmount);
        var isLocationChanged = HasLocationChanged(context) ? 1f : 0f;

        return new FraudModelInput
        {
            NormalizedAmount = normalizedAmount,
            AmountToAverageRatio = amountToAverageRatio,
            RecentTransactionCount = Math.Max(0, context.RecentTransactionCount),
            DistinctLocationCount = Math.Max(0, context.DistinctLocationCount),
            IsLocationChanged = isLocationChanged
        };
    }

    private FraudModelOutput RunInference(FraudModelInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_hasModelArtifact && _predictionEngine is not null)
        {
            lock (_predictionLock)
            {
                return _predictionEngine.Predict(input);
            }
        }

        // Fallback keeps service operational if model is unavailable.
        var linearCombination =
            (input.NormalizedAmount * 1.15f) +
            (MathF.Min(input.AmountToAverageRatio, 5f) * 0.85f) +
            (MathF.Min(input.RecentTransactionCount, 8f) * 0.32f) +
            (MathF.Min(input.DistinctLocationCount, 6f) * 0.24f) +
            (input.IsLocationChanged * 0.9f) -
            1.7f;
        var probability = Sigmoid(linearCombination);

        return new FraudModelOutput
        {
            PredictedLabel = probability >= 0.5f,
            Probability = probability,
            Score = probability
        };
    }

    private static int ConvertProbabilityToRiskScore(float probability) =>
        Math.Clamp((int)Math.Round(Math.Clamp(probability, 0f, 1f) * 100f), 0, 100);

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

    private static bool HasLocationChanged(FraudRiskContext context)
    {
        if (context.LastTransaction is null)
        {
            return false;
        }

        return !string.Equals(
            context.LastTransaction.Location,
            context.Location,
            StringComparison.OrdinalIgnoreCase);
    }

    private static float Sigmoid(float value) => 1f / (1f + MathF.Exp(-value));

    private static string GetRiskLevel(int score) =>
        score switch
        {
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };
}
