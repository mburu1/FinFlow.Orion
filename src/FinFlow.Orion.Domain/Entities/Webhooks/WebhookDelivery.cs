using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Entities.Webhooks;

public sealed class WebhookDelivery : Entity
{
    public Guid WebhookEventId { get; private set; }
    public int AttemptNumber { get; private set; }
    public bool IsSuccessful { get; private set; }
    public int? HttpStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public string? ErrorMessage { get; private set; }
    public TimeSpan Duration { get; private set; }
    public DateTime AttemptedAt { get; private set; }

    private WebhookDelivery() { } // EF Core

    public static WebhookDelivery Create(
        Guid webhookEventId,
        int attemptNumber,
        bool isSuccessful,
        TimeSpan duration,
        int? httpStatusCode = null,
        string? responseBody = null,
        string? errorMessage = null)
    {
        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookEventId = webhookEventId,
            AttemptNumber = attemptNumber,
            IsSuccessful = isSuccessful,
            Duration = duration,
            HttpStatusCode = httpStatusCode,
            ResponseBody = responseBody,
            ErrorMessage = errorMessage,
            AttemptedAt = DateTime.UtcNow
        };
    }
}