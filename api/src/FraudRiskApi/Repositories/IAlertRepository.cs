using FraudRiskApi.Domain.Models;

namespace FraudRiskApi.Repositories;

public interface IAlertRepository
{
    Task AddAsync(Alert alert, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Alert>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}
