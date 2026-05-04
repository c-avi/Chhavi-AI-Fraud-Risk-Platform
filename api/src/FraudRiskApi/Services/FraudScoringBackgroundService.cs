using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class FraudScoringBackgroundService : BackgroundService
{
    private const int PendingRecoveryBatchSize = 500;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFraudScoringQueue _queue;
    private readonly ILogger<FraudScoringBackgroundService> _logger;

    public FraudScoringBackgroundService(
        IServiceScopeFactory scopeFactory,
        IFraudScoringQueue queue,
        ILogger<FraudScoringBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingWorkAsync(stoppingToken);

        try
        {
            await foreach (var transactionId in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessOneAsync(transactionId, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RecoverPendingWorkAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var pending = await repository.GetPendingTransactionIdsAsync(PendingRecoveryBatchSize, cancellationToken);

        foreach (var id in pending)
        {
            await _queue.EnqueueAsync(id, cancellationToken);
        }

        if (pending.Count > 0)
        {
            _logger.LogInformation("Re-queued {Count} pending fraud scoring jobs after startup.", pending.Count);
        }
    }

    private async Task ProcessOneAsync(int transactionId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scoring = scope.ServiceProvider.GetRequiredService<IFraudScoringService>();
            await scoring.ProcessPendingTransactionAsync(transactionId, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Fraud scoring failed for transaction {TransactionId}.", transactionId);
        }
    }
}
