using System;
using System.Collections.Generic;
using System.Text;
using FinFlow.Orion.Domain.Entities.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class ReconciliationItemConfiguration
    : IEntityTypeConfiguration<ReconciliationItem>
{
    public void Configure(EntityTypeBuilder<ReconciliationItem> builder)
    {
        builder.ToTable("ReconciliationItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.ReportId)
            .IsRequired();

        builder.Property(i => i.PaymentReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.ProviderTransactionId)
            .HasMaxLength(100);

        // InternalAmount – owned Money
        builder.OwnsOne(i => i.InternalAmount, money =>
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

        // ProviderAmount – owned Money
        builder.OwnsOne(i => i.ProviderAmount, money =>
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

        builder.Property(i => i.InternalStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.ProviderStatus)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.IsMatched)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.TransactionDate)
            .IsRequired();

        builder.Ignore(i => i.DomainEvents);

        // Indexes
        builder.HasIndex(i => i.ReportId);
        builder.HasIndex(i => i.PaymentReference);
        builder.HasIndex(i => i.ProviderTransactionId);
        builder.HasIndex(i => i.IsMatched);
        builder.HasIndex(i => new { i.ReportId, i.IsMatched });
    }
} 

