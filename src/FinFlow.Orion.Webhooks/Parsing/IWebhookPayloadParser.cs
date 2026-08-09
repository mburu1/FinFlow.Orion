using FinFlow.Orion.Domain.Enums;

namespace FinFlow.Orion.Webhooks.Parsing;

public sealed record ParsedWebhookResult(
    WebhookEventType EventType,
    string? PaymentReference,
    string? ProviderTransactionId);

public interface IWebhookPayloadParser
{
    ParsedWebhookResult Parse(Models.WebhookPayload payload);
}
