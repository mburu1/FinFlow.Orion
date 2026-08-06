namespace FinFlow.Orion.Admin.Models;

public sealed class WebhookViewModel
{
    public Guid WebhookEventId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public string? ProviderTransactionId { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsReplayed { get; set; }
    public int ProcessingAttempts { get; set; }
    public string? ProcessingError { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}