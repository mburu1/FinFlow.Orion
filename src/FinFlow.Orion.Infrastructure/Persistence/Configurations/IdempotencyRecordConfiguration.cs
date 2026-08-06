using FinFlow.Orion.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration
    : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Id)
            .ValueGeneratedNever();

        builder.Property(k => k.Key)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(k => k.Key)
            .IsUnique();

        builder.Property(k => k.Response)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(k => k.CreatedAt)
            .IsRequired();

        builder.Property(k => k.ExpiresAt)
            .IsRequired();

        // Index for cleanup job — WHERE ExpiresAt < GETUTCDATE()
        builder.HasIndex(k => k.ExpiresAt);
    }
}