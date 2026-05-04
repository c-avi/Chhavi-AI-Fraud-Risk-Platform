using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IAlertService
{
    Task CreateAlertIfHighRiskAsync(
        Transaction transaction,
        FraudFeatureSet featureSet,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertResponse>> GetAlertsAsync(int limit = 10, CancellationToken cancellationToken = default);
}
