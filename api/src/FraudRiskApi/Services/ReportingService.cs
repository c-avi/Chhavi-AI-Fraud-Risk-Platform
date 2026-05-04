using FraudRiskApi.Models;
using FraudRiskApi.Repositories;

namespace FraudRiskApi.Services;

public sealed class ReportingService : IReportingService
{
    private readonly ITransactionRepository _transactionRepository;

    public ReportingService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionResponse> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        if (transactionId <= 0)
        {
            throw new ArgumentException("Transaction id must be greater than zero.");
        }

        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);

        if (transaction is null)
        {
            throw new KeyNotFoundException($"Transaction with id {transactionId} was not found.");
        }

        return MapToResponse(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsReportAsync(
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        string? riskLevel,
        CancellationToken cancellationToken = default)
    {
        if (fromTimestamp.HasValue && toTimestamp.HasValue && fromTimestamp.Value > toTimestamp.Value)
        {
            throw new ArgumentException("fromTimestamp must be earlier than or equal to toTimestamp.");
        }

        var transactions = await _transactionRepository.GetReportTransactionsAsync(
            fromTimestamp,
            toTimestamp,
            riskLevel,
            cancellationToken);

        return transactions
            .Select(MapToResponse)
            .ToArray();
    }

    private static TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            TransactionId = transaction.TransactionId,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            Location = transaction.Location,
            Timestamp = transaction.Timestamp,
            RiskScore = transaction.RiskScore,
            RiskLevel = transaction.RiskLevel,
            ScoringStatus = transaction.ScoringStatus.ToString(),
            ScoringError = transaction.ScoringError
        };
    }
}
