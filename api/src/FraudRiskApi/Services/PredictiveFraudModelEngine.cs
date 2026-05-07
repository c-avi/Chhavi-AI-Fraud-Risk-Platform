using FraudRiskApi.Models;
using FraudRiskApi.Models.ML;
using Microsoft.ML;

namespace FraudRiskApi.Services;

public sealed class PredictiveFraudModelEngine : IFraudRiskModelEngine
{
    private const int ErrorScore = -1;
    private readonly string _modelArtifactPath;
    private readonly bool _isModelLoaded;
    private readonly ILogger<PredictiveFraudModelEngine> _logger;
    private readonly PredictionEngine<FraudModelInput, FraudModelOutput>? _predictionEngine;
    private readonly Lock _predictionLock = new();

    public PredictiveFraudModelEngine(IHostEnvironment hostEnvironment, ILogger<PredictiveFraudModelEngine> logger)
    {
        _logger = logger;
        _modelArtifactPath = ResolveModelArtifactPath(hostEnvironment.ContentRootPath);

        if (!File.Exists(_modelArtifactPath))
        {
            _logger.LogError("ML model artifact missing at {ModelArtifactPath}. Prediction engine will not be available.", _modelArtifactPath);
            return;
        }

        try
        {
            var mlContext = new MLContext(seed: 42);
            using var modelStream = File.OpenRead(_modelArtifactPath);
            var transformer = mlContext.Model.Load(modelStream, out _);
            _predictionEngine = mlContext.Model.CreatePredictionEngine<FraudModelInput, FraudModelOutput>(transformer);
            _isModelLoaded = true;
            _logger.LogInformation("ML Model Successfully Loaded from {ModelArtifactPath}.", _modelArtifactPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ML model artifact from {ModelArtifactPath}. Prediction engine unavailable.", _modelArtifactPath);
        }
    }

    public async ValueTask<FraudRiskAssessment> EvaluateAsync(FraudRiskContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var modelInput = BuildModelInput(context);
        var modelOutput = RunInference(modelInput, cancellationToken);
        if (modelOutput is null)
        {
            return new FraudRiskAssessment
            {
                RiskScore = ErrorScore,
                RiskLevel = GetRiskLevel(ErrorScore)
            };
        }

        var probability = ResolveProbability(modelOutput);
        _logger.LogInformation("ML inference raw probability: {RawProbability}", probability);
        var riskScore = ConvertProbabilityToRiskScore(probability);

        return new FraudRiskAssessment
        {
            RiskScore = riskScore,
            RiskLevel = GetRiskLevel(riskScore)
        };
    }

    private static string ResolveModelArtifactPath(string contentRootPath)
    {
        var relativePath = Path.Combine("Models", "ML", "fraud-risk-model.zip");
        var primaryCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        if (File.Exists(primaryCandidate))
        {
            return primaryCandidate;
        }

        var contentRootCandidate = Path.GetFullPath(Path.Combine(contentRootPath, relativePath));
        if (File.Exists(contentRootCandidate))
        {
            return contentRootCandidate;
        }

        // Covers local development where the app runs from bin/* and the model sits under source root.
        var sourceRelativeCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", relativePath));
        if (File.Exists(sourceRelativeCandidate))
        {
            return sourceRelativeCandidate;
        }

        return primaryCandidate;
    }

    private FraudModelInput BuildModelInput(FraudRiskContext context)
    {
        var amountScale = Math.Max((float)context.HistoricalAverageAmount * 4f, 20_000f);
        var normalizedAmount = ApplyLogCompression(NormalizeAmount((float)context.Amount, amountScale));
        var amountToAverageRatio = ApplyLogCompression(GetAmountRatio((float)context.Amount, (float)context.HistoricalAverageAmount));
        var smoothedDistinctLocationCount = ApplyLaplaceLocationSmoothing(context.DistinctLocationCount);
        var isLocationChanged = HasLocationChanged(context)
            ? GetLocationChangeWeight(context.DistinctLocationCount)
            : 0f;

        return new FraudModelInput
        {
            NormalizedAmount = normalizedAmount,
            AmountToAverageRatio = amountToAverageRatio,
            RecentTransactionCount = Math.Max(0, context.RecentTransactionCount),
            DistinctLocationCount = smoothedDistinctLocationCount,
            IsLocationChanged = isLocationChanged
        };
    }

    public FraudFeatureSet GetAuditFeatureSet(FraudRiskContext context)
    {
        var modelInput = BuildModelInput(context);
        var locationChanged = modelInput.IsLocationChanged >= 0.5f;
        return new FraudFeatureSet
        {
            NormalizedAmount = modelInput.NormalizedAmount,
            AmountToAverageRatio = modelInput.AmountToAverageRatio,
            RecentTransactionCount = (int)modelInput.RecentTransactionCount,
            DistinctLocationCount = (int)modelInput.DistinctLocationCount,
            LocationChangedSinceLast = locationChanged,
            GeoVelocity = new GeoVelocitySnapshot
            {
                DistinctLocationsInWindow = (int)modelInput.DistinctLocationCount,
                LocationChangedSinceLastTransaction = locationChanged
            }
        };
    }

    private FraudModelOutput? RunInference(FraudModelInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_isModelLoaded && _predictionEngine is not null)
        {
            lock (_predictionLock)
            {
                return _predictionEngine.Predict(input);
            }
        }

        _logger.LogError("Model inference skipped because the ML model is not loaded.");
        return null;
    }

    private float ResolveProbability(FraudModelOutput output)
    {
        var probability = output.Probability;
        if (probability is >= 0f and <= 1f)
        {
            return probability;
        }

        if (output.Score is >= 0f and <= 1f)
        {
            _logger.LogWarning(
                "FraudModelOutput.Probability is out of range ({Probability}); using Score as probability.",
                output.Probability);
            return output.Score;
        }

        _logger.LogWarning(
            "FraudModelOutput probability-like outputs are invalid (Probability={Probability}, Score={Score}); defaulting to 0.",
            output.Probability,
            output.Score);
        return 0f;
    }

    private static int ConvertProbabilityToRiskScore(float probability)
    {
        var normalized = Math.Clamp(probability, 0f, 1f);
        var scaledScore = Math.Round((decimal)normalized * 100m, 1, MidpointRounding.AwayFromZero);
        return (int)Math.Clamp(decimal.ToInt32(Math.Round(scaledScore, 0, MidpointRounding.AwayFromZero)), 0, 100);
    }

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

    private static float ApplyLogCompression(float value)
    {
        var safe = MathF.Max(0f, value);
        return MathF.Log(1f + safe);
    }

    private static float ApplyLaplaceLocationSmoothing(int distinctLocationCount)
    {
        const float pseudoCount = 1f;
        const float pseudoWindow = 2f;
        var safeCount = MathF.Max(0f, distinctLocationCount);
        return (safeCount + pseudoCount) / (1f + pseudoWindow);
    }

    private static float GetLocationChangeWeight(int distinctLocationCount)
    {
        var safeCount = Math.Max(0, distinctLocationCount);
        return safeCount <= 1 ? 0.45f : 1f;
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

    private static string GetRiskLevel(int score) =>
        score switch
        {
            < 0 => "Unavailable",
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };
}
