namespace FraudRiskApi.Models;

public sealed class TransactionRequest
{
    public string UserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Location { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
