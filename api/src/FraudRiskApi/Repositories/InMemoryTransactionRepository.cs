using System.Collections.Concurrent;
using FraudRiskApi.Models;

namespace FraudRiskApi.Repositories;

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<string, List<Transaction>> _transactionsByUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _sync = new();

    public Task<Transaction?> GetLastTransactionAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_transactionsByUser.TryGetValue(userId, out var entries) || entries.Count == 0)
            {
                return Task.FromResult<Transaction?>(null);
            }

            var lastTransaction = entries.MaxBy(transaction => transaction.Timestamp);
            return Task.FromResult(lastTransaction);
        }
    }

    public Task<int> CountTransactionsSinceAsync(
        string userId,
        DateTime fromTimestamp,
        DateTime toTimestamp,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_transactionsByUser.TryGetValue(userId, out var entries) || entries.Count == 0)
            {
                return Task.FromResult(0);
            }

            var count = entries.Count(transaction =>
                transaction.Timestamp >= fromTimestamp &&
                transaction.Timestamp <= toTimestamp);

            return Task.FromResult(count);
        }
    }

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_transactionsByUser.TryGetValue(transaction.UserId, out var entries))
            {
                entries = [];
                _transactionsByUser[transaction.UserId] = entries;
            }

            entries.Add(transaction);
        }

        return Task.CompletedTask;
    }
}
