using System.Text.Json;
using FraudRiskApi.Domain.Models;
using FraudRiskApi.Models;
using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class AlertService : IAlertService
{
    private const int HighRiskThreshold = 70;
    private static readonly JsonSerializerOptions FeatureJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IAlertRepository _alertRepository;
    private static readonly JsonSerializerOptions ReadFeatureJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AlertService(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task CreateAlertIfHighRiskAsync(
        Transaction transaction,
        FraudFeatureSet featureSet,
        CancellationToken cancellationToken = default)
    {
        if (transaction.RiskScore < HighRiskThreshold)
        {
            return;
        }

        var featureJson = JsonSerializer.Serialize(featureSet, FeatureJsonOptions);

        var alert = new Alert
        {
            TransactionId = transaction.TransactionId,
            RiskScore = transaction.RiskScore,
            Message = $"High-risk transaction flagged with score {transaction.RiskScore}.",
            CreatedAt = DateTime.UtcNow,
            FeatureSetJson = featureJson
        };

        await _alertRepository.AddAsync(alert, cancellationToken);
    }

    public async Task<IReadOnlyList<AlertResponse>> GetAlertsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 100);
        var alerts = await _alertRepository.GetRecentAsync(normalizedLimit, cancellationToken);

        return alerts
            .Select(alert => new AlertResponse
            {
                Id = alert.Id,
                TransactionId = alert.TransactionId,
                RiskScore = alert.RiskScore,
                RiskLevel = ResolveRiskLevel(alert.RiskScore),
                Message = alert.Message,
                CreatedAt = alert.CreatedAt,
                RiskIndicators = BuildRiskIndicators(alert.FeatureSetJson),
                FeatureSetJson = alert.FeatureSetJson
            })
            .ToArray();
    }

    private static string ResolveRiskLevel(int score) =>
        score switch
        {
            >= HighRiskThreshold => "High",
            >= 40 => "Medium",
            _ => "Low"
        };

    private static IReadOnlyList<string> BuildRiskIndicators(string? featureSetJson)
    {
        if (string.IsNullOrWhiteSpace(featureSetJson))
        {
            return new[] { "Model flagged composite anomaly pattern." };
        }

        FraudFeatureSet? features;
        try
        {
            features = JsonSerializer.Deserialize<FraudFeatureSet>(featureSetJson, ReadFeatureJsonOptions);
        }
        catch (JsonException)
        {
            return new[] { "Model flagged composite anomaly pattern." };
        }

        if (features is null)
        {
            return new[] { "Model flagged composite anomaly pattern." };
        }

        var indicators = new List<string>(4);

        if (features.LocationChangedSinceLast || features.GeoVelocity.LocationChangedSinceLastTransaction)
        {
            indicators.Add("Unusual Location");
        }

        if (features.AmountToAverageRatio >= 2.0f)
        {
            indicators.Add("Amount Spike vs Customer Pattern");
        }

        if (features.RecentTransactionCount >= 5)
        {
            indicators.Add("High Transaction Velocity");
        }

        if (features.DistinctLocationCount >= 3)
        {
            indicators.Add("Multiple Locations in Short Window");
        }

        return indicators.Count > 0
            ? indicators
            : new[] { "Model flagged composite anomaly pattern." };
    }
}
