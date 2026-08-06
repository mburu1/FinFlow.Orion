using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Events.Webhooks;

public sealed class WebhookReplayedEvent : DomainEvent
{
    public Guid WebhookEventId { get; }
    public PaymentProvider Provider { get; }
    public string? PaymentReference { get; }

    public WebhookReplayedEvent(
        Guid webhookEventId,
        PaymentProvider provider,
        string? paymentReference)
    {
        WebhookEventId = webhookEventId;
        Provider = provider;
        PaymentReference = paymentReference;
    }
}