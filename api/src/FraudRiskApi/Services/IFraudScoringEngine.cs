using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IFraudScoringEngine
{
    RiskScoreResponse CalculateScore(TransactionRequest request);
}
