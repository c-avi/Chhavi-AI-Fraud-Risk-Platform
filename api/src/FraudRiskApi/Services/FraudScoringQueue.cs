using System.Threading.Channels;

namespace FraudRiskApi.Services;

public sealed class FraudScoringQueue : IFraudScoringQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(int transactionId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(transactionId, cancellationToken);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
