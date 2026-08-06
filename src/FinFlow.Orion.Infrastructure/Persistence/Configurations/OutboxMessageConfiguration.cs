using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrionOutbox = FinFlow.Orion.Infrastructure.Persistence.Outbox.OutboxMessage;
using OutboxMessageStatus = FinFlow.Orion.Infrastructure.Persistence.Outbox.OutboxMessageStatus;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OrionOutbox>
{
    public void Configure(EntityTypeBuilder<OrionOutbox> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(OutboxMessageStatus.Pending);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.ProcessedAt);

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        builder.Property(o => o.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.NextRetryAt);

        builder.Property(o => o.AggregateId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.AggregateType)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);
        builder.HasIndex(o => new { o.Status, o.NextRetryAt });
    }
}