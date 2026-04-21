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

    public Task<Transaction?> GetLastTransactionAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId)
            .OrderByDescending(transaction => transaction.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
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

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
