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
            .Where(transaction => transaction.UserId == userId)
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
                transaction.Timestamp >= fromTimestamp &&
                transaction.Timestamp <= toTimestamp)
            .Select(transaction => transaction.Location)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalTransactionsProcessed = await _dbContext.Transactions.CountAsync(cancellationToken);
        var highRiskAlertsCount = await _dbContext.Transactions.CountAsync(
            transaction => transaction.RiskLevel == "High",
            cancellationToken);
        var averageFraudRiskScore = totalTransactionsProcessed == 0
            ? 0m
            : decimal.Round(
                await _dbContext.Transactions.AverageAsync(transaction => (decimal)transaction.RiskScore, cancellationToken),
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
            .OrderByDescending(transaction => transaction.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
