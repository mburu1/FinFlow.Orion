using FinFlow.Orion.Application.Sagas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public sealed class PaymentSagaStateConfiguration : IEntityTypeConfiguration<PaymentSagaState>
{
    public void Configure(EntityTypeBuilder<PaymentSagaState> builder)
    {
        builder.ToTable("PaymentSagaStates");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.PaymentId)
            .IsRequired();

        // Looked up frequently to find the active saga for a payment.
        builder.HasIndex(s => s.PaymentId);

        builder.Property(s => s.CurrentStep)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.LastFailureReason)
            .HasMaxLength(1000);

        builder.Property(s => s.FallbackProvider)
            .HasMaxLength(30);

        builder.Property(s => s.CompletedSteps)
            .HasConversion(
                steps => string.Join(',', steps),
                csv => string.IsNullOrEmpty(csv)
                    ? new List<string>()
                    : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new ValueComparer<List<string>>(
                    (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                    v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                    v => v.ToList()))
            .HasMaxLength(500);

        builder.Property(s => s.StartedAt)
            .IsRequired();
    }
}
