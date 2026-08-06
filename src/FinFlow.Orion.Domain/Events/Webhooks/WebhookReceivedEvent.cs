using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Events.Webhooks;

public sealed class WebhookReceivedEvent : DomainEvent
{
    public Guid WebhookEventId { get; }
    public PaymentProvider Provider { get; }
    public WebhookEventType EventType { get; }
    public string? PaymentReference { get; }

    public WebhookReceivedEvent(
        Guid webhookEventId,
        PaymentProvider provider,
        WebhookEventType eventType,
        string? paymentReference)
    {
        WebhookEventId = webhookEventId;
        Provider = provider;
        EventType = eventType;
        PaymentReference = paymentReference;
    }
}