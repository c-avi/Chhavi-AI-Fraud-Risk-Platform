using System.Collections.Concurrent;
using FraudRiskApi.Domain.Models;
using FraudRiskApi.Models;

namespace FraudRiskApi.Repositories;

public sealed class InMemoryScoringSubmissionStore : IScoringSubmissionStore
{
    private readonly InMemoryTransactionRepository _transactions;
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _idempotency = new(StringComparer.Ordinal);

    public InMemoryScoringSubmissionStore(InMemoryTransactionRepository transactions)
    {
        _transactions = transactions;
    }

    public async Task<BeginScoringResult> TryBeginOrResolveAsync(
        TransactionRequest request,
        string idempotencyKey,
        string requestContentHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_idempotency.TryGetValue(idempotencyKey, out var entry))
        {
            if (!string.Equals(entry.RequestContentHash, requestContentHash, StringComparison.Ordinal))
            {
                return new BeginScoringResult
                {
                    Kind = BeginScoringKind.Conflict,
                    TransactionId = entry.TransactionId
                };
            }

            var linked = await _transactions.GetByIdAsync(entry.TransactionId, cancellationToken);
            if (linked is null)
            {
                throw new InvalidOperationException("Idempotency row references a missing transaction.");
            }

            if (linked.ScoringStatus == TransactionScoringStatus.Completed)
            {
                return new BeginScoringResult
                {
                    Kind = BeginScoringKind.CompletedReplay,
                    TransactionId = linked.TransactionId,
                    CompletedScore = MapScore(linked)
                };
            }

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

        await _transactions.AddAsync(pending, cancellationToken);

        if (!_idempotency.TryAdd(
                idempotencyKey,
                new IdempotencyEntry(requestContentHash, pending.TransactionId)))
        {
            throw new InvalidOperationException("Concurrent idempotency collision.");
        }

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

    private readonly record struct IdempotencyEntry(string RequestContentHash, int TransactionId);
}
