using FinFlow.Orion.Domain.Entities.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
            .ValueGeneratedNever();

        builder.Property(j => j.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(j => j.PaymentReference)
            .HasMaxLength(100);

        // TotalAmount — owned Money value object
        builder.OwnsOne(j => j.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalAmount")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            money.Property(m => m.CurrencyCode)
                .HasColumnName("TotalCurrencyCode")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(j => j.PostedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(j => j.PostedAt)
            .IsRequired();

        builder.Property(j => j.IsBalanced)
            .IsRequired()
            .HasDefaultValue(false);

        // Entries — one-to-many
        builder.HasMany(j => j.Entries)
            .WithOne()
            .HasForeignKey(e => e.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(j => j.DomainEvents);

        builder.HasIndex(j => j.PaymentReference);
        builder.HasIndex(j => j.PostedAt);
    }
}