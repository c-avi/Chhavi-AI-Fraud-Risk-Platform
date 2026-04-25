using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IReportingService
{
    Task<TransactionResponse> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionResponse>> GetTransactionsReportAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        string? riskLevel,
        CancellationToken cancellationToken = default);
}
