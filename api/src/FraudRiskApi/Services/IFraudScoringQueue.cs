namespace FraudRiskApi.Services;

public interface IFraudScoringQueue
{
    ValueTask EnqueueAsync(int transactionId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken);
}
