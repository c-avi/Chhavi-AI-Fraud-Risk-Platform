namespace FraudRiskApi.Models.ML;

public sealed class FraudModelOutput
{
    public required float PredictedFraudProbability { get; init; }
}
