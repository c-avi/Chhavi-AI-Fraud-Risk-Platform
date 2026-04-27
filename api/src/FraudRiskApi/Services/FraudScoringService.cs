using FraudRiskApi.Models;
using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class FraudScoringService : IFraudScoringService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFraudRiskModelEngine _fraudRiskModelEngine;

    public FraudScoringService(
        ITransactionRepository transactionRepository,
        IFraudRiskModelEngine fraudRiskModelEngine)
    {
        _transactionRepository = transactionRepository;
        _fraudRiskModelEngine = fraudRiskModelEngine;
    }

    public async Task<RiskScoreResponse> ScoreTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var lastTransaction = await _transactionRepository.GetLastTransactionAsync(request.UserId, cancellationToken);
        var recentWindowStart = request.Timestamp.AddMinutes(-15);
        var historyWindowStart = request.Timestamp.AddDays(-30);
        var recentTransactionCount = await _transactionRepository.CountTransactionsSinceAsync(
            request.UserId,
            recentWindowStart,
            request.Timestamp,
            cancellationToken);
        var historicalAverageAmount = await _transactionRepository.GetAverageAmountAsync(
            request.UserId,
            historyWindowStart,
            request.Timestamp,
            cancellationToken);
        var distinctLocationCount = await _transactionRepository.CountDistinctLocationsSinceAsync(
            request.UserId,
            historyWindowStart,
            request.Timestamp,
            cancellationToken);

        var assessment = await _fraudRiskModelEngine.EvaluateAsync(
            new FraudRiskContext
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Location = request.Location,
                Timestamp = request.Timestamp,
                LastTransaction = lastTransaction,
                RecentTransactionCount = recentTransactionCount,
                HistoricalAverageAmount = historicalAverageAmount,
                DistinctLocationCount = distinctLocationCount
            },
            cancellationToken);

        var transaction = new Transaction
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Location = request.Location,
            Timestamp = request.Timestamp,
            RiskScore = assessment.RiskScore,
            RiskLevel = assessment.RiskLevel
        };

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        return new RiskScoreResponse
        {
            TransactionId = transaction.TransactionId,
            RiskScore = transaction.RiskScore,
            RiskLevel = transaction.RiskLevel
        };
    }

    public Task<RiskSummaryResponse> GetRiskSummaryAsync(CancellationToken cancellationToken = default) =>
        _transactionRepository.GetRiskSummaryAsync(cancellationToken);

    public async Task<IReadOnlyList<TransactionAlertResponse>> GetRecentAlertsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetRecentTransactionsAsync(limit, cancellationToken);

        return transactions
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
