namespace FraudRiskApi.Models.ML;

public sealed class FraudModelInput
{
    public required float NormalizedAmount { get; init; }
    public required float AmountToAverageRatio { get; init; }
    public required float RecentTransactionCount { get; init; }
    public required float DistinctLocationCount { get; init; }
    public required float IsLocationChanged { get; init; }
}
