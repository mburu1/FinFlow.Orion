using FinFlow.Orion.Domain.Entities.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class DiscrepancyConfiguration : IEntityTypeConfiguration<Discrepancy>
{
    public void Configure(EntityTypeBuilder<Discrepancy> builder)
    {
        builder.ToTable("Discrepancies");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.ReportId)
            .IsRequired();

        builder.Property(d => d.PaymentReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.DiscrepancyType)
            .HasMaxLength(50)
            .IsRequired();

        // InternalAmount — owned Money
        builder.OwnsOne(d => d.InternalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("InternalAmount")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("InternalCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        // ProviderAmount — owned Money
        builder.OwnsOne(d => d.ProviderAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("ProviderAmount")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("ProviderCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        // DifferenceAmount — owned Money
        builder.OwnsOne(d => d.DifferenceAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("DifferenceAmount")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("DifferenceCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(d => d.Notes)
            .HasMaxLength(1000);

        builder.Property(d => d.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.ResolvedBy)
            .HasMaxLength(100);

        builder.Property(d => d.DetectedAt)
            .IsRequired();

        builder.HasIndex(d => d.ReportId);
        builder.HasIndex(d => d.PaymentReference);
        builder.HasIndex(d => d.IsResolved);
    }
}