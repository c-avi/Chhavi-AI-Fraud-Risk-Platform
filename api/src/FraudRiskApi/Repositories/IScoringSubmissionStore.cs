using FraudRiskApi.Models;

namespace FraudRiskApi.Repositories;

public interface IScoringSubmissionStore
{
    Task<BeginScoringResult> TryBeginOrResolveAsync(
        TransactionRequest request,
        string idempotencyKey,
        string requestContentHash,
        CancellationToken cancellationToken = default);
}
