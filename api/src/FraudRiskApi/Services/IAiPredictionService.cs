using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IAiPredictionService
{
    Task<AiPredictionResponse> PredictAsync(AiPredictionRequest request, CancellationToken cancellationToken = default);
}

