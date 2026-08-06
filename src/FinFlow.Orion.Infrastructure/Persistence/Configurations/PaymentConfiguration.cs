using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        // Reference — owned value object
        builder.OwnsOne(p => p.Reference, refNav =>
        {
            refNav.Property(r => r.Reference)
                .HasColumnName("Reference")
                .HasMaxLength(50)
                .IsRequired();

            refNav.HasIndex(r => r.Reference)
                .IsUnique();
        });

        // Money — owned value object (Amount + CurrencyCode)
        builder.OwnsOne(p => p.Amount, money =>
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

        // IdempotencyKey — owned value object
        builder.OwnsOne(p => p.IdempotencyKey, ik =>
        {
            ik.Property(k => k.Value)
                .HasColumnName("IdempotencyKey")
                .HasMaxLength(256)
                .IsRequired();

            ik.HasIndex(k => k.Value)
                .IsUnique();
        });

        // PhoneNumber — optional owned value object
        builder.OwnsOne(p => p.PhoneNumber, ph =>
        {
            ph.Property(n => n.Number)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(20);

            ph.Property(n => n.CountryCode)
                .HasColumnName("PhoneCountryCode")
                .HasMaxLength(5);
        });

        // ProviderResponse — optional owned value object
        builder.OwnsOne(p => p.ProviderResponse, pr =>
        {
            pr.Property(r => r.ProviderTransactionId)
                .HasColumnName("ProviderTransactionId")
                .HasMaxLength(256);

            pr.Property(r => r.Status)
                .HasColumnName("ProviderStatus")
                .HasMaxLength(50);

            pr.Property(r => r.Message)
                .HasColumnName("ProviderMessage")
                .HasMaxLength(500);

            pr.Property(r => r.ReceivedAt)
                .HasColumnName("ProviderResponseAt");
        });

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Provider)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CustomerId)
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100);

        // Attempts — one-to-many
        builder.HasMany(p => p.Attempts)
            .WithOne()
            .HasForeignKey(a => a.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events — not persisted to relational DB
        builder.Ignore(p => p.DomainEvents);

        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.CustomerId);
    }
}