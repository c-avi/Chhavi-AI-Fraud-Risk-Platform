using System.Collections.Concurrent;
using FraudRiskApi.Models;

namespace FraudRiskApi.Repositories;

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<string, List<Transaction>> _transactionsByUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _sync = new();
    private int _nextTransactionId = 1;

    private static bool IsSettled(Transaction transaction) =>
        transaction.ScoringStatus == TransactionScoringStatus.Completed;

    public Task<Transaction?> GetByIdAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var transaction = _transactionsByUser.Values
                .SelectMany(entries => entries)
                .FirstOrDefault(entry => entry.TransactionId == transactionId);

            return Task.FromResult(transaction);
        }
    }

    public Task<Transaction?> GetLastTransactionAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_transactionsByUser.TryGetValue(userId, out var entries) || entries.Count == 0)
            {
                return Task.FromResult<Transaction?>(null);
            }

            var settled = entries.Where(IsSettled).ToList();
            var lastTransaction = settled.Count == 0
                ? null
                : settled.MaxBy(transaction => transaction.Timestamp);
            return Task.FromResult(lastTransaction);
        }
    }

    public Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var transactions = _transactionsByUser.Values
                .SelectMany(entries => entries)
                .Where(transaction =>
                    IsSettled(transaction) &&
                    (!fromTimestamp.HasValue || transaction.Timestamp >= fromTimestamp.Value) &&
                    (!toTimestamp.HasValue || transaction.Timestamp <= toTimestamp.Value))
                .OrderByDescending(transaction => transaction.Timestamp)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Transaction>>(transactions);
        }
    }

    public Task<IReadOnlyList<Transaction>> GetReportTransactionsAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        string? riskLevel,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedRiskLevel = string.IsNullOrWhiteSpace(riskLevel)
            ? null
            : riskLevel.Trim();

        lock (_sync)
        {
            var transactions = _transactionsByUser.Values
                .SelectMany(entries => entries)
                .Where(transaction =>
                    IsSettled(transaction) &&
                    (!fromTimestamp.HasValue || transaction.Timestamp >= fromTimestamp.Value) &&
                    (!toTimestamp.HasValue || transaction.Timestamp <= toTimestamp.Value) &&
                    (normalizedRiskLevel is null ||
                     string.Equals(transaction.RiskLevel, normalizedRiskLevel, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(transaction => transaction.Timestamp)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Transaction>>(transactions);
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
                IsSettled(transaction) &&
                transaction.Timestamp >= fromTimestamp &&
                transaction.Timestamp <= toTimestamp);

            return Task.FromResult(count);
        }
    }

    public Task<decimal> GetAverageAmountAsync(
        string userId,
        DateTime fromTimestamp,
        DateTime toTimestamp,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_transactionsByUser.TryGetValue(userId, out var entries))
            {
                return Task.FromResult(0m);
            }

            var matchingEntries = entries
                .Where(transaction =>
                    IsSettled(transaction) &&
                    transaction.Timestamp >= fromTimestamp &&
                    transaction.Timestamp <= toTimestamp)
                .ToArray();

            if (matchingEntries.Length == 0)
            {
                return Task.FromResult(0m);
            }

            return Task.FromResult(matchingEntries.Average(transaction => transaction.Amount));
        }
    }

    public Task<int> CountDistinctLocationsSinceAsync(
        string userId,
        DateTime fromTimestamp,
        DateTime toTimestamp,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_transactionsByUser.TryGetValue(userId, out var entries))
            {
                return Task.FromResult(0);
            }

            var count = entries
                .Where(transaction =>
                    IsSettled(transaction) &&
                    transaction.Timestamp >= fromTimestamp &&
                    transaction.Timestamp <= toTimestamp)
                .Select(transaction => transaction.Location)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return Task.FromResult(count);
        }
    }

    public Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var settled = _transactionsByUser.Values
                .SelectMany(entries => entries)
                .Where(IsSettled)
                .ToArray();

            return Task.FromResult(new RiskSummaryResponse
            {
                TotalTransactionsProcessed = settled.Length,
                HighRiskAlertsCount = settled.Count(transaction => string.Equals(transaction.RiskLevel, "High", StringComparison.OrdinalIgnoreCase)),
                AverageFraudRiskScore = settled.Length == 0
                    ? 0m
                    : decimal.Round((decimal)settled.Average(transaction => transaction.RiskScore), 1)
            });
        }
    }

    public Task<IReadOnlyList<Transaction>> GetRecentTransactionsAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var transactions = _transactionsByUser.Values
                .SelectMany(entries => entries)
                .Where(IsSettled)
                .OrderByDescending(transaction => transaction.Timestamp)
                .Take(limit)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Transaction>>(transactions);
        }
    }

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (transaction.TransactionId == 0)
            {
                transaction.TransactionId = _nextTransactionId++;
            }

            if (!_transactionsByUser.TryGetValue(transaction.UserId, out var entries))
            {
                entries = [];
                _transactionsByUser[transaction.UserId] = entries;
            }

            entries.Add(transaction);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            foreach (var entries in _transactionsByUser.Values)
            {
                var index = entries.FindIndex(t => t.TransactionId == transaction.TransactionId);
                if (index >= 0)
                {
                    entries[index] = transaction;
                    return Task.CompletedTask;
                }
            }
        }

        throw new KeyNotFoundException($"Transaction {transaction.TransactionId} was not found for update.");
    }

    public Task<IReadOnlyList<int>> GetPendingTransactionIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedTake = Math.Clamp(take, 1, 10_000);

        lock (_sync)
        {
            var ids = _transactionsByUser.Values
                .SelectMany(entries => entries)
                .Where(t => t.ScoringStatus == TransactionScoringStatus.Pending)
                .OrderBy(t => t.TransactionId)
                .Take(normalizedTake)
                .Select(t => t.TransactionId)
                .ToArray();

            return Task.FromResult<IReadOnlyList<int>>(ids);
        }
    }
}
