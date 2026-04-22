using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public interface IFraudRiskModelEngine
{
    ValueTask<FraudRiskAssessment> EvaluateAsync(FraudRiskContext context, CancellationToken cancellationToken = default);
}
