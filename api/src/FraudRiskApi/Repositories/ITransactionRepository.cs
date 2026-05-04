using FraudRiskApi.Models;

namespace FraudRiskApi.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int transactionId, CancellationToken cancellationToken = default);
    Task<Transaction?> GetLastTransactionAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetReportTransactionsAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        string? riskLevel,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        CancellationToken cancellationToken = default);
    Task<int> CountTransactionsSinceAsync(string userId, DateTime fromTimestamp, DateTime toTimestamp, CancellationToken cancellationToken = default);
    Task<decimal> GetAverageAmountAsync(string userId, DateTime fromTimestamp, DateTime toTimestamp, CancellationToken cancellationToken = default);
    Task<int> CountDistinctLocationsSinceAsync(string userId, DateTime fromTimestamp, DateTime toTimestamp, CancellationToken cancellationToken = default);
    Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetRecentTransactionsAsync(int limit, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetPendingTransactionIdsAsync(int take, CancellationToken cancellationToken = default);
}
