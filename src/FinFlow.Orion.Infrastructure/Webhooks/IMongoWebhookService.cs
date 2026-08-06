using FinFlow.Orion.Domain.Entities.Webhooks;
using FinFlow.Orion.Domain.Enums;

namespace FinFlow.Orion.Infrastructure.Webhooks;

public interface IMongoWebhookService
{
    /// <summary>
    /// Persists the raw webhook payload to MongoDB as-is.
    /// Schema differs per provider — NoSQL is the correct choice here.
    /// </summary>
    Task StoreRawPayloadAsync(
        Guid webhookEventId,
        PaymentProvider provider,
        string rawPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the raw payload for a given webhook event ID.
    /// Used by the admin panel to inspect original provider data.
    /// </summary>
    Task<WebhookRawDocument?> GetRawPayloadAsync(
        Guid webhookEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all unprocessed raw webhook documents for a given provider.
    /// Used by the reconciliation job to cross-check with the ledger.
    /// </summary>
    Task<IReadOnlyList<WebhookRawDocument>> GetUnprocessedByProviderAsync(
        PaymentProvider provider,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a raw document as processed in MongoDB.
    /// </summary>
    Task MarkAsProcessedAsync(
        Guid webhookEventId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// MongoDB document model for raw webhook payloads.
/// Each provider has a different schema — stored as raw JSON string.
/// </summary>
public sealed class WebhookRawDocument
{
    public Guid WebhookEventId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string RawPayload { get; init; } = string.Empty;
    public bool IsProcessed { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}