using FraudRiskApi.Models;
using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class FraudScoringService : IFraudScoringService
{
    private const decimal HighAmountThreshold = 10_000m;
    private const int HighAmountRisk = 30;
    private const int UnusualLocationRisk = 40;
    private const int RapidFrequencyRisk = 30;

    private readonly ITransactionRepository _transactionRepository;

    public FraudScoringService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<RiskScoreResponse> ScoreTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var score = 0;

        if (request.Amount > HighAmountThreshold)
        {
            score += HighAmountRisk;
        }

        var lastTransaction = await _transactionRepository.GetLastTransactionAsync(request.UserId, cancellationToken);
        if (lastTransaction is not null &&
            !string.Equals(lastTransaction.Location, request.Location, StringComparison.OrdinalIgnoreCase))
        {
            score += UnusualLocationRisk;
        }

        var fromTimestamp = request.Timestamp.AddMinutes(-1);
        var transactionCountInWindow = await _transactionRepository.CountTransactionsSinceAsync(
            request.UserId,
            fromTimestamp,
            request.Timestamp,
            cancellationToken);

        if (transactionCountInWindow > 0)
        {
            score += RapidFrequencyRisk;
        }

        var transaction = new Transaction
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Location = request.Location,
            Timestamp = request.Timestamp,
            RiskScore = score,
            RiskLevel = GetRiskLevel(score)
        };

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        return new RiskScoreResponse
        {
            RiskScore = transaction.RiskScore,
            RiskLevel = transaction.RiskLevel
        };
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

    private static string GetRiskLevel(int score) =>
        score switch
        {
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };
}
