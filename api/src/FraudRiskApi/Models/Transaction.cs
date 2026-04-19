namespace FraudRiskApi.Models;

public sealed class Transaction
{
    public string UserId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Location { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
