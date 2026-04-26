using System.Collections.Concurrent;
using FraudRiskApi.Models;

namespace FraudRiskApi.Repositories;

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<string, List<Transaction>> _transactionsByUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _sync = new();

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

            var lastTransaction = entries.MaxBy(transaction => transaction.Timestamp);
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
                .Where(transaction => transaction.Timestamp >= fromTimestamp && transaction.Timestamp <= toTimestamp)
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
                .Where(transaction => transaction.Timestamp >= fromTimestamp && transaction.Timestamp <= toTimestamp)
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
            var allTransactions = _transactionsByUser.Values.SelectMany(entries => entries).ToArray();

            return Task.FromResult(new RiskSummaryResponse
            {
                TotalTransactionsProcessed = allTransactions.Length,
                HighRiskAlertsCount = allTransactions.Count(transaction => string.Equals(transaction.RiskLevel, "High", StringComparison.OrdinalIgnoreCase)),
                AverageFraudRiskScore = allTransactions.Length == 0
                    ? 0m
                    : decimal.Round((decimal)allTransactions.Average(transaction => transaction.RiskScore), 1)
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
