using FinFlow.Orion.Domain.Entities.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class ReconciliationReportConfiguration
    : IEntityTypeConfiguration<ReconciliationReport>
{
    public void Configure(EntityTypeBuilder<ReconciliationReport> builder)
    {
        builder.ToTable("ReconciliationReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.ReportReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(r => r.ReportReference)
            .IsUnique();

        builder.Property(r => r.Provider)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.ReconDate)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.TotalTransactions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.MatchedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.UnmatchedCount)
            .IsRequired()
            .HasDefaultValue(0);

        // TotalMatchedAmount — owned Money
        builder.OwnsOne(r => r.TotalMatchedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalMatchedAmount")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("MatchedCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        // TotalDiscrepancyAmount — owned Money
        builder.OwnsOne(r => r.TotalDiscrepancyAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalDiscrepancyAmount")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("DiscrepancyCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100);

        builder.Property(r => r.UpdatedBy)
            .HasMaxLength(100);

        // Items — one-to-many
        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        // Discrepancies — one-to-many
        builder.HasMany(r => r.Discrepancies)
            .WithOne()
            .HasForeignKey(d => d.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.DomainEvents);

        builder.HasIndex(r => r.Provider);
        builder.HasIndex(r => r.ReconDate);
        builder.HasIndex(r => new { r.Provider, r.ReconDate });
    }
}