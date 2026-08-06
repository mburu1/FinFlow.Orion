using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Events.Webhooks;
using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Entities.Webhooks;

public sealed class WebhookEvent : AggregateRoot
{
    public PaymentProvider Provider { get; private set; }
    public WebhookEventType EventType { get; private set; }
    public string RawPayload { get; private set; } = null!;     // Stored as-is → MongoDB
    public string? PaymentReference { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public bool IsProcessed { get; private set; }
    public bool IsReplayed { get; private set; }
    public int ProcessingAttempts { get; private set; }
    public string? ProcessingError { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private readonly List<WebhookDelivery> _deliveries = [];
    public IReadOnlyCollection<WebhookDelivery> Deliveries => _deliveries.AsReadOnly();

    private WebhookEvent() { } // EF Core

    public static WebhookEvent Create(
        PaymentProvider provider,
        WebhookEventType eventType,
        string rawPayload,
        string? paymentReference = null,
        string? providerTransactionId = null)
    {
        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            EventType = eventType,
            RawPayload = rawPayload,
            PaymentReference = paymentReference,
            ProviderTransactionId = providerTransactionId,
            IsProcessed = false,
            IsReplayed = false,
            ProcessingAttempts = 0,
            ReceivedAt = DateTime.UtcNow
        };

        webhookEvent.AddDomainEvent(new WebhookReceivedEvent(
            webhookEvent.Id,
            provider,
            eventType,
            paymentReference));

        return webhookEvent;
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
        ProcessingAttempts++;
    }

    public void MarkAsFailed(string error)
    {
        ProcessingError = error;
        ProcessingAttempts++;
    }

    public void Replay()
    {
        IsProcessed = false;
        IsReplayed = true;
        ProcessingError = null;
        ProcessingAttempts = 0;

        AddDomainEvent(new WebhookReplayedEvent(Id, Provider, PaymentReference));
    }

    public void AddDelivery(WebhookDelivery delivery) => _deliveries.Add(delivery);
}