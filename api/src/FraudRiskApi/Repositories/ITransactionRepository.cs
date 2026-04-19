using FraudRiskApi.Models;

namespace FraudRiskApi.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetLastTransactionAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> CountTransactionsSinceAsync(string userId, DateTime fromTimestamp, DateTime toTimestamp, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
