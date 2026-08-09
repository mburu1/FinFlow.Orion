using System.Text.Json;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Webhooks.Models;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Webhooks.Parsing;

/// <summary>
/// Parses a provider's raw webhook body into a normalized (event type, payment
/// reference, provider transaction id) result the downstream consumer can act on.
/// </summary>
public sealed class WebhookPayloadParser : IWebhookPayloadParser
{
    private readonly ILogger<WebhookPayloadParser> _logger;

    public WebhookPayloadParser(ILogger<WebhookPayloadParser> logger)
        => _logger = logger;

    public ParsedWebhookResult Parse(WebhookPayload payload) => payload.Provider switch
    {
        PaymentProvider.MPesa => ParseMpesa(payload),
        PaymentProvider.Card or PaymentProvider.BankTransfer => ParseSimulated(payload),
        _ => new ParsedWebhookResult(WebhookEventType.PaymentFailed, null, null)
    };

    /// <summary>
    /// Real Safaricom Daraja STK Push callback shape:
    /// { "Body": { "stkCallback": { "CheckoutRequestID", "ResultCode", "CallbackMetadata": { "Item": [...] } } } }
    /// ResultCode 0 = success; any other value = failure. Daraja does not echo our
    /// internal payment reference, so correlation happens via CheckoutRequestID,
    /// which we stored as the payment's ProviderTransactionId at dispatch time.
    /// </summary>
    private ParsedWebhookResult ParseMpesa(WebhookPayload payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload.RawBody);
            var stkCallback = document.RootElement.GetProperty("Body").GetProperty("stkCallback");

            var checkoutRequestId = stkCallback.TryGetProperty("CheckoutRequestID", out var checkoutIdProp)
                ? checkoutIdProp.GetString()
                : null;

            var resultCode = stkCallback.TryGetProperty("ResultCode", out var resultCodeProp)
                ? resultCodeProp.GetInt32()
                : -1;

            var eventType = resultCode == 0 ? WebhookEventType.PaymentCompleted : WebhookEventType.PaymentFailed;

            return new ParsedWebhookResult(eventType, PaymentReference: null, ProviderTransactionId: checkoutRequestId);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning(ex,
                "[WebhookPayloadParser] Failed to parse M-Pesa STK callback body for webhook {Id} — treating as failed.",
                payload.WebhookEventId);

            return new ParsedWebhookResult(WebhookEventType.PaymentFailed, null, null);
        }
    }

    /// <summary>
    /// Documented placeholder contract for the Card/Bank simulator providers, since
    /// no real gateway is wired for either yet (see PaymentProviderDispatcher —
    /// Card completes synchronously and never calls back; Bank is a stub too).
    /// Shape: { "transactionId": "...", "status": "captured"|"failed", "reference": "..." }
    /// Replace once a real Card/Bank gateway is integrated.
    /// </summary>
    private ParsedWebhookResult ParseSimulated(WebhookPayload payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload.RawBody);
            var root = document.RootElement;

            var transactionId = root.TryGetProperty("transactionId", out var txProp) ? txProp.GetString() : null;
            var reference = root.TryGetProperty("reference", out var refProp) ? refProp.GetString() : null;
            var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

            var eventType = string.Equals(status, "captured", StringComparison.OrdinalIgnoreCase)
                ? WebhookEventType.PaymentCompleted
                : WebhookEventType.PaymentFailed;

            return new ParsedWebhookResult(eventType, reference, transactionId);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "[WebhookPayloadParser] Failed to parse simulated {Provider} callback body for webhook {Id} — treating as failed.",
                payload.Provider, payload.WebhookEventId);

            return new ParsedWebhookResult(WebhookEventType.PaymentFailed, null, null);
        }
    }
}
