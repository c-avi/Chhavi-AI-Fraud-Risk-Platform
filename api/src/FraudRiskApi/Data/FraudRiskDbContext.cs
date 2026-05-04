using FraudRiskApi.Models;
using FraudRiskApi.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudRiskApi.Data;

public sealed class FraudRiskDbContext : DbContext
{
    public FraudRiskDbContext(DbContextOptions<FraudRiskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

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

            entity.Property(transaction => transaction.ScoringStatus)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(transaction => transaction.ScoringError)
                .HasMaxLength(2000);

            entity.HasIndex(transaction => new { transaction.UserId, transaction.Timestamp });
            entity.HasIndex(transaction => transaction.ScoringStatus);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasKey(alert => alert.Id);

            entity.Property(alert => alert.Id)
                .ValueGeneratedOnAdd();

            entity.Property(alert => alert.TransactionId)
                .IsRequired();

            entity.Property(alert => alert.RiskScore)
                .IsRequired();

            entity.Property(alert => alert.Message)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(alert => alert.CreatedAt)
                .IsRequired();

            entity.Property(alert => alert.FeatureSetJson)
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(alert => alert.CreatedAt);

            entity.HasOne<Transaction>()
                .WithMany()
                .HasForeignKey(alert => alert.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("IdempotencyRecords");
            entity.HasKey(record => record.Id);

            entity.Property(record => record.Id)
                .ValueGeneratedOnAdd();

            entity.Property(record => record.Key)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(record => record.RequestContentHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(record => record.TransactionId)
                .IsRequired();

            entity.Property(record => record.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(record => record.Key).IsUnique();

            entity.HasOne<Transaction>()
                .WithMany()
                .HasForeignKey(record => record.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
