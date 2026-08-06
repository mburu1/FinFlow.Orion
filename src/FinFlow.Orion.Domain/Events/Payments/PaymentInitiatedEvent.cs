using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Events.Payments;

public sealed class PaymentInitiatedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public string Reference { get; }
    public Money Amount { get; }
    public PaymentProvider Provider { get; }
    public string IdempotencyKey { get; }

    public PaymentInitiatedEvent(
        Guid paymentId,
        string reference,
        Money amount,
        PaymentProvider provider,
        string idempotencyKey)
    {
        PaymentId = paymentId;
        Reference = reference;
        Amount = amount;
        Provider = provider;
        IdempotencyKey = idempotencyKey;
    }
}