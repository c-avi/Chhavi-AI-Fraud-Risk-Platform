using FraudRiskApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudRiskApi.Data;

public sealed class FraudRiskDbContext : DbContext
{
    public FraudRiskDbContext(DbContextOptions<FraudRiskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(transaction => transaction.TransactionId);

            entity.Property(transaction => transaction.TransactionId)
                .ValueGeneratedOnAdd();

            entity.Property(transaction => transaction.UserId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(transaction => transaction.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(transaction => transaction.Location)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(transaction => transaction.Timestamp)
                .IsRequired();

            entity.Property(transaction => transaction.RiskScore)
                .IsRequired();

            entity.Property(transaction => transaction.RiskLevel)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(transaction => new { transaction.UserId, transaction.Timestamp });
        });
    }
}
