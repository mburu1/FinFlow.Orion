namespace FinFlow.Orion.Contracts.Webhooks.Responses;

public sealed class WebhookEventResponse
{
    public Guid WebhookEventId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string? PaymentReference { get; init; }
    public string? ProviderTransactionId { get; init; }
    public bool IsProcessed { get; init; }
    public bool IsReplayed { get; init; }
    public int ProcessingAttempts { get; init; }
    public string? ProcessingError { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}