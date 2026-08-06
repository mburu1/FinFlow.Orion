using FinFlow.Orion.Domain.Entities.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.JournalEntryId)
            .IsRequired();

        builder.Property(e => e.AccountId)
            .IsRequired();

        builder.Property(e => e.EntryType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        // Amount — owned Money value object
        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("CurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(e => e.Narration)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.ReferenceId)
            .HasMaxLength(100);

        builder.Property(e => e.PostedAt)
            .IsRequired();

        builder.HasIndex(e => e.JournalEntryId);
        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.ReferenceId);
        builder.HasIndex(e => e.PostedAt);
    }
}