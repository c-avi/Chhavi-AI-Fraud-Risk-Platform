namespace FraudRiskApi.Models;

public sealed class TransactionResponse
{
    public int TransactionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
