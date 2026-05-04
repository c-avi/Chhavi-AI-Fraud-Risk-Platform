namespace FraudRiskApi.Domain.Models;

public sealed class Alert
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int RiskScore { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? FeatureSetJson { get; set; }
}
