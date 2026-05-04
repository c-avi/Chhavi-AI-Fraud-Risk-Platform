namespace FraudRiskApi.Domain.Models;

public sealed class IdempotencyRecord
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string RequestContentHash { get; set; } = string.Empty;
    public int TransactionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
