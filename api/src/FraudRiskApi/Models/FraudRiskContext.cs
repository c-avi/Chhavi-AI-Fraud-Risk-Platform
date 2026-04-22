namespace FraudRiskApi.Models;

public sealed class FraudRiskContext
{
    public required string UserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Location { get; init; }
    public required DateTime Timestamp { get; init; }
    public Transaction? LastTransaction { get; init; }
    public int RecentTransactionCount { get; init; }
    public decimal HistoricalAverageAmount { get; init; }
    public int DistinctLocationCount { get; init; }
}
