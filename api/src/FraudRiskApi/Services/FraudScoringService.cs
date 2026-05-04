using FraudRiskApi.Models;
using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class FraudScoringService : IFraudScoringService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFraudRiskModelEngine _fraudRiskModelEngine;
    private readonly IAlertService _alertService;
    private readonly IScoringSubmissionStore _scoringSubmissionStore;
    private readonly IFraudScoringQueue _fraudScoringQueue;

    public FraudScoringService(
        ITransactionRepository transactionRepository,
        IFraudRiskModelEngine fraudRiskModelEngine,
        IAlertService alertService,
        IScoringSubmissionStore scoringSubmissionStore,
        IFraudScoringQueue fraudScoringQueue)
    {
        _transactionRepository = transactionRepository;
        _fraudRiskModelEngine = fraudRiskModelEngine;
        _alertService = alertService;
        _scoringSubmissionStore = scoringSubmissionStore;
        _fraudScoringQueue = fraudScoringQueue;
    }

    public async Task<BeginScoringResult> SubmitTransactionForScoringAsync(
        TransactionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ValidateRequest(request);
        var requestHash = TransactionRequestFingerprint.Compute(request);
        var result = await _scoringSubmissionStore.TryBeginOrResolveAsync(
            request,
            idempotencyKey.Trim(),
            requestHash,
            cancellationToken);

        if (result.Kind is BeginScoringKind.CreatedPending or BeginScoringKind.AlreadyPending)
        {
            await _fraudScoringQueue.EnqueueAsync(result.TransactionId, cancellationToken);
        }

        return result;
    }

    public async Task ProcessPendingTransactionAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null || transaction.ScoringStatus != TransactionScoringStatus.Pending)
        {
            return;
        }

        try
        {
            var lastTransaction = await _transactionRepository.GetLastTransactionAsync(transaction.UserId, cancellationToken);
            var recentWindowStart = transaction.Timestamp.AddMinutes(-15);
            var historyWindowStart = transaction.Timestamp.AddDays(-30);
            var recentTransactionCount = await _transactionRepository.CountTransactionsSinceAsync(
                transaction.UserId,
                recentWindowStart,
                transaction.Timestamp,
                cancellationToken);
            var historicalAverageAmount = await _transactionRepository.GetAverageAmountAsync(
                transaction.UserId,
                historyWindowStart,
                transaction.Timestamp,
                cancellationToken);
            var distinctLocationCount = await _transactionRepository.CountDistinctLocationsSinceAsync(
                transaction.UserId,
                historyWindowStart,
                transaction.Timestamp,
                cancellationToken);

            var context = new FraudRiskContext
            {
                UserId = transaction.UserId,
                Amount = transaction.Amount,
                Location = transaction.Location,
                Timestamp = transaction.Timestamp,
                LastTransaction = lastTransaction,
                RecentTransactionCount = recentTransactionCount,
                HistoricalAverageAmount = historicalAverageAmount,
                DistinctLocationCount = distinctLocationCount
            };

            var assessment = await _fraudRiskModelEngine.EvaluateAsync(context, cancellationToken);
            var featureSet = _fraudRiskModelEngine.GetAuditFeatureSet(context);

            transaction.RiskScore = assessment.RiskScore;
            transaction.RiskLevel = assessment.RiskLevel;
            transaction.ScoringStatus = TransactionScoringStatus.Completed;
            transaction.ScoringError = null;

            await _transactionRepository.UpdateAsync(transaction, cancellationToken);
            await _alertService.CreateAlertIfHighRiskAsync(transaction, featureSet, cancellationToken);
        }
        catch (Exception ex)
        {
            var failed = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
            if (failed is null || failed.ScoringStatus != TransactionScoringStatus.Pending)
            {
                return;
            }

            failed.RiskLevel = "Unavailable";
            failed.ScoringStatus = TransactionScoringStatus.Failed;
            failed.ScoringError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

            await _transactionRepository.UpdateAsync(failed, cancellationToken);
        }
    }

    public Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default) =>
        _transactionRepository.GetRiskSummaryAsync(cancellationToken);

    public async Task<IReadOnlyList<TransactionAlertResponse>> GetRecentAlertsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        const int highRiskThreshold = 70;
        var transactions = await _transactionRepository.GetRecentTransactionsAsync(limit, cancellationToken);

        return transactions
            .Where(transaction => transaction.RiskScore >= highRiskThreshold)
            .Select(transaction => new TransactionAlertResponse
            {
                UserId = transaction.UserId,
                RiskScore = transaction.RiskScore,
                RiskLevel = transaction.RiskLevel,
                Timestamp = transaction.Timestamp
            })
            .ToArray();
    }

    private static void ValidateRequest(TransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            throw new ArgumentException("Location is required.");
        }

        if (request.Timestamp == default)
        {
            throw new ArgumentException("Timestamp is required.");
        }
    }
}
