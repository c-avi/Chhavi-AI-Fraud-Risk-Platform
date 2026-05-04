namespace FraudRiskApi.Models;

public sealed class AcceptedTransactionResponse
{
    public int TransactionId { get; init; }
    public string Status { get; init; } = "accepted";
    public string StatusUrl { get; init; } = string.Empty;
}
