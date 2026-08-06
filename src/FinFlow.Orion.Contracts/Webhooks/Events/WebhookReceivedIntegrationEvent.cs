namespace FinFlow.Orion.Contracts.Webhooks.Events;

public sealed class WebhookReceivedIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid WebhookEventId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string? PaymentReference { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}