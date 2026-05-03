namespace FraudRiskApi.Models;

public sealed class AlertResponse
{
    public int Id { get; init; }
    public int TransactionId { get; init; }
    public int RiskScore { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
