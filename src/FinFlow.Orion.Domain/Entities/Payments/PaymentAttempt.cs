using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Payments;

public sealed class PaymentAttempt : Entity
{
    public Guid PaymentId { get; private set; }
    public int AttemptNumber { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Result { get; private set; }
    public string? FailureReason { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public DateTime AttemptedAt { get; private set; }
    public TimeSpan? Duration { get; private set; }

    private PaymentAttempt() { } // EF Core

    public static PaymentAttempt Create(
        Guid paymentId,
        int attemptNumber,
        PaymentProvider provider,
        PaymentStatus result,
        string? providerTransactionId = null,
        string? failureReason = null,
        TimeSpan? duration = null)
    {
        return new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            AttemptNumber = attemptNumber,
            Provider = provider,
            Result = result,
            ProviderTransactionId = providerTransactionId,
            FailureReason = failureReason,
            AttemptedAt = DateTime.UtcNow,
            Duration = duration
        };
    }

    public bool IsSuccessful => Result == PaymentStatus.Captured;
}