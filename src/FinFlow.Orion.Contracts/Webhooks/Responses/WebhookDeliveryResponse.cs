namespace FinFlow.Orion.Contracts.Webhooks.Responses;

public sealed class WebhookDeliveryResponse
{
    public Guid DeliveryId { get; init; }
    public Guid WebhookEventId { get; init; }
    public int AttemptNumber { get; init; }
    public bool IsSuccessful { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? ErrorMessage { get; init; }
    public double DurationMs { get; init; }
    public DateTime AttemptedAt { get; init; }
}