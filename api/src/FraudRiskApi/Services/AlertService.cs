using FraudRiskApi.Domain.Models;
using FraudRiskApi.Models;
using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class AlertService : IAlertService
{
    private const int HighRiskThreshold = 70;
    private readonly IAlertRepository _alertRepository;

    public AlertService(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task CreateAlertIfHighRiskAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction.RiskScore < HighRiskThreshold)
        {
            return;
        }

        var alert = new Alert
        {
            TransactionId = transaction.TransactionId,
            RiskScore = transaction.RiskScore,
            Message = $"High-risk transaction flagged with score {transaction.RiskScore}.",
            CreatedAt = DateTime.UtcNow
        };

        await _alertRepository.AddAsync(alert, cancellationToken);
    }

    public async Task<IReadOnlyList<AlertResponse>> GetAlertsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 100);
        var alerts = await _alertRepository.GetRecentAsync(normalizedLimit, cancellationToken);

        return alerts
            .Select(alert => new AlertResponse
            {
                Id = alert.Id,
                TransactionId = alert.TransactionId,
                RiskScore = alert.RiskScore,
                Message = alert.Message,
                CreatedAt = alert.CreatedAt
            })
            .ToArray();
    }
}
