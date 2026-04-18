namespace FraudRiskApi.Models;

public sealed class TransactionRequest
{
    public decimal Amount { get; set; }

    /// <summary>
    /// When true, the transaction originates from a location not previously seen for this customer.
    /// </summary>
    public bool IsNewLocation { get; set; }

    /// <summary>
    /// Number of transactions in the recent window (e.g. last hour) used to evaluate velocity.
    /// </summary>
    public int TransactionsInLastHour { get; set; }
}
