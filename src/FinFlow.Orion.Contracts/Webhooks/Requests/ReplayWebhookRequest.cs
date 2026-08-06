namespace FinFlow.Orion.Contracts.Webhooks.Requests;

public sealed class ReplayWebhookRequest
{
    public Guid WebhookEventId { get; init; }
    public string ReplayedBy { get; init; } = string.Empty;
    public string? Reason { get; init; }
}