using FraudRiskApi.Data;
using FraudRiskApi.Domain.Models;
using FraudRiskApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudRiskApi.Repositories;

public sealed class SqlScoringSubmissionStore : IScoringSubmissionStore
{
    private readonly FraudRiskDbContext _dbContext;

    public SqlScoringSubmissionStore(FraudRiskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<BeginScoringResult> TryBeginOrResolveAsync(
        TransactionRequest request,
        string idempotencyKey,
        string requestContentHash,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => ExecuteInTransactionAsync(request, idempotencyKey, requestContentHash, cancellationToken));
    }

    private async Task<BeginScoringResult> ExecuteInTransactionAsync(
        TransactionRequest request,
        string idempotencyKey,
        string requestContentHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existing = await _dbContext.IdempotencyRecords
            .AsQueryable()
            .FirstOrDefaultAsync(record => record.Key == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestContentHash, requestContentHash, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new BeginScoringResult
                {
                    Kind = BeginScoringKind.Conflict,
                    TransactionId = existing.TransactionId
                };
            }

            var linked = await _dbContext.Transactions
                .FirstAsync(t => t.TransactionId == existing.TransactionId, cancellationToken);

            if (linked.ScoringStatus == TransactionScoringStatus.Completed)
            {
                await transaction.CommitAsync(cancellationToken);
                return new BeginScoringResult
                {
                    Kind = BeginScoringKind.CompletedReplay,
                    TransactionId = linked.TransactionId,
                    CompletedScore = MapScore(linked)
                };
            }

            await transaction.CommitAsync(cancellationToken);
            return new BeginScoringResult
            {
                Kind = BeginScoringKind.AlreadyPending,
                TransactionId = linked.TransactionId
            };
        }

        var pending = new Transaction
        {
            UserId = request.UserId.Trim(),
            Amount = request.Amount,
            Location = request.Location.Trim(),
            Timestamp = request.Timestamp,
            RiskScore = 0,
            RiskLevel = "Pending",
            ScoringStatus = TransactionScoringStatus.Pending
        };

        await _dbContext.Transactions.AddAsync(pending, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.IdempotencyRecords.AddAsync(
            new IdempotencyRecord
            {
                Key = idempotencyKey,
                RequestContentHash = requestContentHash,
                TransactionId = pending.TransactionId,
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new BeginScoringResult
        {
            Kind = BeginScoringKind.CreatedPending,
            TransactionId = pending.TransactionId
        };
    }

    private static RiskScoreResponse MapScore(Transaction transaction)
    {
        return new RiskScoreResponse
        {
            TransactionId = transaction.TransactionId,
            RiskScore = transaction.RiskScore,
            RiskLevel = transaction.RiskLevel
        };
    }
}
