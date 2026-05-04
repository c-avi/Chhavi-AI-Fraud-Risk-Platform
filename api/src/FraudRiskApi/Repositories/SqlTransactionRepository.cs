using FraudRiskApi.Data;
using FraudRiskApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudRiskApi.Repositories;

public sealed class SqlTransactionRepository : ITransactionRepository
{
    private readonly FraudRiskDbContext _dbContext;

    public SqlTransactionRepository(FraudRiskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Transaction?> GetByIdAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(transaction => transaction.TransactionId == transactionId, cancellationToken);
    }

    public Task<Transaction?> GetLastTransactionAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.ScoringStatus == TransactionScoringStatus.Completed)
            .OrderByDescending(transaction => transaction.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Transactions
            .AsNoTracking()
            .AsQueryable();

        if (fromTimestamp.HasValue)
        {
            query = query.Where(transaction => transaction.Timestamp >= fromTimestamp.Value);
        }

        if (toTimestamp.HasValue)
        {
            query = query.Where(transaction => transaction.Timestamp <= toTimestamp.Value);
        }

        return await query
            .Where(transaction => transaction.ScoringStatus == TransactionScoringStatus.Completed)
            .OrderByDescending(transaction => transaction.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetReportTransactionsAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        string? riskLevel,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Transactions
            .AsNoTracking()
            .AsQueryable();

        if (fromTimestamp.HasValue)
        {
            query = query.Where(transaction => transaction.Timestamp >= fromTimestamp.Value);
        }

        if (toTimestamp.HasValue)
        {
            query = query.Where(transaction => transaction.Timestamp <= toTimestamp.Value);
        }

        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            var normalizedRiskLevel = riskLevel.Trim();
            query = query.Where(transaction => transaction.RiskLevel == normalizedRiskLevel);
        }

        return await query
            .Where(transaction => transaction.ScoringStatus == TransactionScoringStatus.Completed)
            .OrderByDescending(transaction => transaction.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountTransactionsSinceAsync(
        string userId,
        DateTime fromTimestamp,
        DateTime toTimestamp,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Transactions
            .AsNoTracking()
            .CountAsync(transaction =>
                transaction.UserId == userId &&
                transaction.ScoringStatus == TransactionScoringStatus.Completed &&
                transaction.Timestamp >= fromTimestamp &&
                transaction.Timestamp <= toTimestamp,
                cancellationToken);
    }

    public async Task<decimal> GetAverageAmountAsync(
        string userId,
        DateTime fromTimestamp,
        DateTime toTimestamp,
        CancellationToken cancellationToken = default)
    {
        var amounts = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.ScoringStatus == TransactionScoringStatus.Completed &&
                transaction.Timestamp >= fromTimestamp &&
                transaction.Timestamp <= toTimestamp)
            .Select(transaction => transaction.Amount)
            .ToListAsync(cancellationToken);

        return amounts.Count == 0 ? 0m : amounts.Average();
    }

    public Task<int> CountDistinctLocationsSinceAsync(
        string userId,
        DateTime fromTimestamp,
        DateTime toTimestamp,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.ScoringStatus == TransactionScoringStatus.Completed &&
                transaction.Timestamp >= fromTimestamp &&
                transaction.Timestamp <= toTimestamp)
            .Select(transaction => transaction.Location)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default)
    {
        var settled = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.ScoringStatus == TransactionScoringStatus.Completed);

        var totalTransactionsProcessed = await settled.CountAsync(cancellationToken);
        var highRiskAlertsCount = await settled.CountAsync(
            transaction => transaction.RiskLevel == "High",
            cancellationToken);
        var averageFraudRiskScore = totalTransactionsProcessed == 0
            ? 0m
            : decimal.Round(
                await settled.AverageAsync(transaction => (decimal)transaction.RiskScore, cancellationToken),
                1);

        return new RiskSummaryResponse
        {
            TotalTransactionsProcessed = totalTransactionsProcessed,
            HighRiskAlertsCount = highRiskAlertsCount,
            AverageFraudRiskScore = averageFraudRiskScore
        };
    }

    public async Task<IReadOnlyList<Transaction>> GetRecentTransactionsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.ScoringStatus == TransactionScoringStatus.Completed)
            .OrderByDescending(transaction => transaction.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _dbContext.Transactions.Update(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetPendingTransactionIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 10_000);
        return await _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.ScoringStatus == TransactionScoringStatus.Pending)
            .OrderBy(t => t.TransactionId)
            .Take(normalizedTake)
            .Select(t => t.TransactionId)
            .ToListAsync(cancellationToken);
    }
}
