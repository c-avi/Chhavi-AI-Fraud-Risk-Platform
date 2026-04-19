using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IFraudScoringService
{
    Task<RiskScoreResponse> ScoreTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default);
}
