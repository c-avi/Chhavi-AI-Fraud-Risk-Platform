namespace FraudRiskApi.Models;

/// <summary>
/// Feature snapshot persisted with high-risk alerts for regulatory audit (model inputs / derived risk factors).
/// </summary>
public sealed class FraudFeatureSet
{
    public float NormalizedAmount { get; init; }
    public float AmountToAverageRatio { get; init; }
    public int RecentTransactionCount { get; init; }
    public int DistinctLocationCount { get; init; }
    public bool LocationChangedSinceLast { get; init; }

    /// <summary>
    /// Geo velocity proxy: distinct locations in the assessment window plus whether the user moved since the last settled transaction.
    /// </summary>
    public GeoVelocitySnapshot GeoVelocity { get; init; } = new();
}

public sealed class GeoVelocitySnapshot
{
    public int DistinctLocationsInWindow { get; init; }
    public bool LocationChangedSinceLastTransaction { get; init; }
}
