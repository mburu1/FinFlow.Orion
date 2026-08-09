using FinFlow.Orion.Contracts.Payments.Events;
using FinFlow.Orion.Contracts.Webhooks.Events;
using FinFlow.Orion.Domain.Abstractions;
using FinFlow.Orion.Domain.Events.Payments;
using FinFlow.Orion.Domain.Events.Webhooks;

namespace FinFlow.Orion.Infrastructure.Messaging;

/// <summary>
/// Maps internal Domain events to the public Contracts integration events that get
/// written to the outbox and, later, published onto the bus. Keeping this mapping
/// explicit (rather than publishing domain events directly) means the bus only ever
/// carries the stable, versioned wire contracts in FinFlow.Orion.Contracts — never
/// internal Domain types.
/// </summary>
public static class IntegrationEventMap
{
    public static (string TypeName, object Payload) Map(IDomainEvent domainEvent) => domainEvent switch
    {
        PaymentInitiatedEvent e => (
            typeof(PaymentInitiatedIntegrationEvent).FullName!,
            new PaymentInitiatedIntegrationEvent
            {
                PaymentId = e.PaymentId,
                Reference = e.Reference,
                Amount = e.Amount.Amount,
                CurrencyCode = e.Amount.CurrencyCode,
                Provider = e.Provider.ToString(),
                IdempotencyKey = e.IdempotencyKey
            }),

        PaymentCompletedEvent e => (
            typeof(PaymentCompletedIntegrationEvent).FullName!,
            new PaymentCompletedIntegrationEvent
            {
                PaymentId = e.PaymentId,
                Reference = e.Reference,
                Amount = e.Amount.Amount,
                CurrencyCode = e.Amount.CurrencyCode,
                Provider = e.Provider.ToString(),
                ProviderTransactionId = e.ProviderTransactionId
            }),

        PaymentFailedEvent e => (
            typeof(PaymentFailedIntegrationEvent).FullName!,
            new PaymentFailedIntegrationEvent
            {
                PaymentId = e.PaymentId,
                Reference = e.Reference,
                Amount = e.Amount.Amount,
                CurrencyCode = e.Amount.CurrencyCode,
                Provider = e.Provider.ToString(),
                FailureReason = e.FailureReason
            }),

        PaymentReversedEvent e => (
            typeof(PaymentReversedIntegrationEvent).FullName!,
            new PaymentReversedIntegrationEvent
            {
                PaymentId = e.PaymentId,
                Reference = e.Reference,
                Amount = e.Amount.Amount,
                CurrencyCode = e.Amount.CurrencyCode,
                Provider = e.Provider.ToString(),
                Reason = e.Reason
            }),

        WebhookReceivedEvent e => (
            typeof(WebhookReceivedIntegrationEvent).FullName!,
            new WebhookReceivedIntegrationEvent
            {
                WebhookEventId = e.WebhookEventId,
                Provider = e.Provider.ToString(),
                EventType = e.EventType.ToString(),
                PaymentReference = e.PaymentReference
            }),

        // Unmapped domain events still make it to the outbox (for audit/history)
        // but WorkerOutboxPublisher will skip publishing them — see TypeRegistry.
        _ => (domainEvent.GetType().FullName!, domainEvent)
    };

    public static readonly IReadOnlyDictionary<string, Type> TypeRegistry = new Dictionary<string, Type>
    {
        [typeof(PaymentInitiatedIntegrationEvent).FullName!] = typeof(PaymentInitiatedIntegrationEvent),
        [typeof(PaymentCompletedIntegrationEvent).FullName!] = typeof(PaymentCompletedIntegrationEvent),
        [typeof(PaymentFailedIntegrationEvent).FullName!] = typeof(PaymentFailedIntegrationEvent),
        [typeof(PaymentReversedIntegrationEvent).FullName!] = typeof(PaymentReversedIntegrationEvent),
        [typeof(WebhookReceivedIntegrationEvent).FullName!] = typeof(WebhookReceivedIntegrationEvent),
    };
}
