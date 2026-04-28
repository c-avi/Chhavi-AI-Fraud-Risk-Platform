using FraudRiskApi.Models;
using FraudRiskApi.Models.ML;

namespace FraudRiskApi.Services;

public sealed class PredictiveFraudModelEngine : IFraudRiskModelEngine
{
    private const float MinimumAmountScale = 20_000f;
    private readonly string _modelArtifactPath;
    private readonly bool _hasModelArtifact;
    private readonly ILogger<PredictiveFraudModelEngine> _logger;

    public PredictiveFraudModelEngine(IHostEnvironment hostEnvironment, ILogger<PredictiveFraudModelEngine> logger)
    {
        _logger = logger;
        _modelArtifactPath = Path.Combine(hostEnvironment.ContentRootPath, "Models", "ML", "fraud-risk-model.zip");
        _hasModelArtifact = File.Exists(_modelArtifactPath);
    }

    public async ValueTask<FraudRiskAssessment> EvaluateAsync(FraudRiskContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var modelInput = BuildModelInput(context);
        var modelOutput = RunMockInference(modelInput, cancellationToken);

        var riskScore = Math.Clamp((int)Math.Round(modelOutput.PredictedFraudProbability * 100f), 0, 100);

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

    private FraudModelOutput RunMockInference(FraudModelInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Simulated ensemble output approximating tree-based feature interactions.
        var linearCombination =
            (input.NormalizedAmount * 1.15f) +
            (MathF.Min(input.AmountToAverageRatio, 5f) * 0.85f) +
            (MathF.Min(input.RecentTransactionCount, 8f) * 0.32f) +
            (MathF.Min(input.DistinctLocationCount, 6f) * 0.24f) +
            (input.IsLocationChanged * 0.9f) -
            1.7f;

        var probability = Sigmoid(linearCombination);

        if (_hasModelArtifact)
        {
            _logger.LogDebug("Model artifact found at {ModelArtifactPath}; using mock predictive fallback until ML runtime integration.", _modelArtifactPath);
            probability = Math.Clamp(probability + 0.02f, 0f, 1f);
        }

        return new FraudModelOutput
        {
            PredictedFraudProbability = probability
        };
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
