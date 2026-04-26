using FraudRiskApi.Models;
using FraudRiskApi.Repositories;
using FraudRiskApi.Services;

namespace FraudRiskApi.Tests;

public sealed class ReportingServiceTests
{
    [Fact]
    public async Task GetTransactionsReportAsync_RiskLevelFilter_IsCaseInsensitive()
    {
        var repository = new InMemoryTransactionRepository();
        await repository.AddAsync(new Transaction
        {
            TransactionId = 1,
            UserId = "user-501",
            Amount = 1500.50m,
            Location = "Mumbai",
            Timestamp = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            RiskScore = 82,
            RiskLevel = "High"
        });
        await repository.AddAsync(new Transaction
        {
            TransactionId = 2,
            UserId = "user-502",
            Amount = 220.75m,
            Location = "Pune",
            Timestamp = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc),
            RiskScore = 18,
            RiskLevel = "Low"
        });

        var service = new ReportingService(repository);

        var result = await service.GetTransactionsReportAsync(
            new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
            " high ",
            CancellationToken.None);

        var transaction = Assert.Single(result);
        Assert.Equal(1, transaction.TransactionId);
        Assert.Equal("High", transaction.RiskLevel);
        Assert.Equal(1500.50m, transaction.Amount);
    }

    [Fact]
    public async Task GetTransactionsReportAsync_DateRangeFilter_ReturnsOnlyMatchingTransactions()
    {
        var repository = new InMemoryTransactionRepository();
        await repository.AddAsync(new Transaction
        {
            TransactionId = 11,
            UserId = "user-201",
            Amount = 3500.25m,
            Location = "Delhi",
            Timestamp = new DateTime(2026, 4, 5, 8, 0, 0, DateTimeKind.Utc),
            RiskScore = 76,
            RiskLevel = "High"
        });
        await repository.AddAsync(new Transaction
        {
            TransactionId = 12,
            UserId = "user-202",
            Amount = 420.10m,
            Location = "Noida",
            Timestamp = new DateTime(2026, 4, 14, 11, 30, 0, DateTimeKind.Utc),
            RiskScore = 22,
            RiskLevel = "Low"
        });
        await repository.AddAsync(new Transaction
        {
            TransactionId = 13,
            UserId = "user-203",
            Amount = 900.00m,
            Location = "Gurugram",
            Timestamp = new DateTime(2026, 4, 26, 14, 45, 0, DateTimeKind.Utc),
            RiskScore = 61,
            RiskLevel = "Medium"
        });

        var service = new ReportingService(repository);

        var result = await service.GetTransactionsReportAsync(
            fromTimestamp: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            toTimestamp: new DateTime(2026, 4, 20, 23, 59, 59, DateTimeKind.Utc),
            riskLevel: null,
            CancellationToken.None);

        var transaction = Assert.Single(result);
        Assert.Equal(12, transaction.TransactionId);
        Assert.Equal(420.10m, transaction.Amount);
    }
}
