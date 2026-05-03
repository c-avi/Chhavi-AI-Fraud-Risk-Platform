using FraudRiskApi.Data;
using FraudRiskApi.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudRiskApi.Repositories;

public sealed class SqlAlertRepository : IAlertRepository
{
    private readonly FraudRiskDbContext _dbContext;

    public SqlAlertRepository(FraudRiskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _dbContext.Alerts.AddAsync(alert, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .AsNoTracking()
            .OrderByDescending(alert => alert.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
