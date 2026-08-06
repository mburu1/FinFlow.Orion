using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Events.Payments;

public sealed class PaymentFailedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public string Reference { get; }
    public Money Amount { get; }
    public PaymentProvider Provider { get; }
    public string FailureReason { get; }

    public PaymentFailedEvent(
        Guid paymentId,
        string reference,
        Money amount,
        PaymentProvider provider,
        string failureReason)
    {
        PaymentId = paymentId;
        Reference = reference;
        Amount = amount;
        Provider = provider;
        FailureReason = failureReason;
    }
}