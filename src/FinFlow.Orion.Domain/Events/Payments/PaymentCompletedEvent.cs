using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Events.Payments;

public sealed class PaymentCompletedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public string Reference { get; }
    public Money Amount { get; }
    public PaymentProvider Provider { get; }
    public string ProviderTransactionId { get; }

    public PaymentCompletedEvent(
        Guid paymentId,
        string reference,
        Money amount,
        PaymentProvider provider,
        string providerTransactionId)
    {
        PaymentId = paymentId;
        Reference = reference;
        Amount = amount;
        Provider = provider;
        ProviderTransactionId = providerTransactionId;
    }
}