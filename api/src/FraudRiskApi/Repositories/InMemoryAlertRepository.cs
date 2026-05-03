using FraudRiskApi.Domain.Models;

namespace FraudRiskApi.Repositories;

public sealed class InMemoryAlertRepository : IAlertRepository
{
    private readonly List<Alert> _alerts = [];
    private readonly Lock _sync = new();
    private int _nextId = 1;

    public Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            alert.Id = _nextId++;
            _alerts.Add(alert);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Alert>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var alerts = _alerts
                .OrderByDescending(alert => alert.CreatedAt)
                .Take(limit)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Alert>>(alerts);
        }
    }
}
