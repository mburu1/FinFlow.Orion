using FinFlow.Orion.Domain.Entities.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("WebhookEvents");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        builder.Property(w => w.Provider)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(w => w.EventType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // RawPayload stored in SQL Server for indexing/querying
        // Full raw JSON also mirrored to MongoDB in MongoWebhookService
        builder.Property(w => w.RawPayload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(w => w.PaymentReference)
            .HasMaxLength(100);

        builder.Property(w => w.ProviderTransactionId)
            .HasMaxLength(256);

        builder.Property(w => w.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(w => w.IsReplayed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(w => w.ProcessingAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(w => w.ProcessingError)
            .HasMaxLength(2000);

        builder.Property(w => w.ReceivedAt)
            .IsRequired();

        // Deliveries — one-to-many
        builder.HasMany(w => w.Deliveries)
            .WithOne()
            .HasForeignKey(d => d.WebhookEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(w => w.DomainEvents);

        builder.HasIndex(w => w.Provider);
        builder.HasIndex(w => w.PaymentReference);
        builder.HasIndex(w => w.IsProcessed);
        builder.HasIndex(w => w.ReceivedAt);
    }
}