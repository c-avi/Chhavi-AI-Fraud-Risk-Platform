using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IFraudScoringService
{
    Task<BeginScoringResult> SubmitTransactionForScoringAsync(
        TransactionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task ProcessPendingTransactionAsync(int transactionId, CancellationToken cancellationToken = default);

    Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionAlertResponse>> GetRecentAlertsAsync(int limit = 10, CancellationToken cancellationToken = default);
}
