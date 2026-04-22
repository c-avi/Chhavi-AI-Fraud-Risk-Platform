using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IFraudScoringService
{
    Task<RiskScoreResponse> ScoreTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default);
    Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionAlertResponse>> GetRecentAlertsAsync(int limit = 10, CancellationToken cancellationToken = default);
}
