using FinFlow.Orion.Domain.Enums;

namespace FinFlow.Orion.Webhooks.Models;

/// <summary>
/// Represents an inbound webhook payload as received from a payment provider,
/// before validation or persistence. The raw body is preserved verbatim so it
/// can be stored as-is (Mongo) and independently signature-verified — never
/// mutate RawBody after capture, since signature checks depend on exact bytes.
/// </summary>
public sealed class WebhookPayload
{
    /// <summary>
    /// The payment provider this webhook originated from (M-Pesa, Card, Bank, etc.).
    /// Determined by the inbound route, e.g. POST /api/webhooks/mpesa.
    /// </summary>
    public required PaymentProvider Provider { get; init; }

    /// <summary>
    /// The exact raw request body, unparsed. Required for signature verification
    /// where providers sign over the literal byte stream, not a re-serialized DTO.
    /// </summary>
    public required string RawBody { get; init; }

    /// <summary>
    /// The provider-supplied signature header value, if present. Null when the
    /// provider does not sign webhooks (validated separately per-provider).
    /// </summary>
    public string? Signature { get; init; }

    /// <summary>
    /// All inbound HTTP headers, preserved for audit/debugging and for providers
    /// that split signature material across multiple headers (e.g. timestamp + sig).
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// UTC timestamp when this webhook was received by our API, independent of
    /// any timestamp the provider embeds in the payload itself.
    /// </summary>
    public required DateTime ReceivedAt { get; init; }

    /// <summary>
    /// Correlation id assigned on receipt, used to trace this webhook through
    /// logs, Mongo raw storage, the outbox, and eventual reconciliation.
    /// </summary>
    public Guid WebhookEventId { get; init; } = Guid.NewGuid();
}