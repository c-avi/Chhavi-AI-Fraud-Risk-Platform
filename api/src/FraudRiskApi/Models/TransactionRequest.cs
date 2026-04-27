using System.ComponentModel.DataAnnotations;

namespace FraudRiskApi.Models;

public sealed class TransactionRequest
{
    [Required]
    [MinLength(1)]
    public string UserId { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    [MinLength(1)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; }
}
