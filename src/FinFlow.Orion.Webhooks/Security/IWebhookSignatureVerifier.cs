using System.Security.Cryptography;
using System.Text;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Webhooks.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinFlow.Orion.Webhooks.Security;

public interface IWebhookSignatureVerifier
{
    bool Verify(WebhookPayload payload);
}

/// <summary>
/// Real per-provider webhook authenticity checks. M-Pesa Daraja does not sign
/// callbacks the way Stripe-style providers do, so it's verified via a shared
/// secret header instead; Card and Bank use HMAC-SHA256 over the raw body.
/// </summary>
public sealed class WebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private readonly WebhookSecurityConfiguration _config;
    private readonly ILogger<WebhookSignatureVerifier> _logger;

    public WebhookSignatureVerifier(
        IOptions<WebhookSecurityConfiguration> config,
        ILogger<WebhookSignatureVerifier> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public bool Verify(WebhookPayload payload) => payload.Provider switch
    {
        PaymentProvider.MPesa => VerifyMpesa(payload),
        PaymentProvider.Card => VerifyHmac(payload, _config.Card.SigningSecret),
        PaymentProvider.BankTransfer => VerifyHmac(payload, _config.Bank.SigningSecret),
        _ => Reject(payload, $"No signature verification strategy is configured for provider '{payload.Provider}'.")
    };

    private bool VerifyMpesa(WebhookPayload payload)
    {
        if (string.IsNullOrEmpty(_config.MPesa.SharedSecret))
        {
            _logger.LogWarning(
                "[WebhookSignatureVerifier] Webhooks:Security:MPesa:SharedSecret is not configured — " +
                "accepting M-Pesa webhook {Id} unverified. Set a shared secret before handling real traffic.",
                payload.WebhookEventId);
            return true;
        }

        var provided = payload.Headers.GetValueOrDefault("X-MPesa-Secret");
        if (string.IsNullOrEmpty(provided))
            return Reject(payload, "Missing X-MPesa-Secret header.");

        if (!FixedTimeEqualsExact(provided, _config.MPesa.SharedSecret))
            return Reject(payload, "X-MPesa-Secret header did not match the configured shared secret.");

        return true;
    }

    private bool VerifyHmac(WebhookPayload payload, string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            return Reject(payload, $"No signing secret is configured for provider '{payload.Provider}'.");

        if (string.IsNullOrEmpty(payload.Signature))
            return Reject(payload, "Missing X-Signature header.");

        var computedHex = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload.RawBody)));

        // Hex casing is not semantically significant, so normalize before the
        // constant-time comparison — a case mismatch should not fail verification.
        if (!FixedTimeEqualsExact(computedHex, payload.Signature.ToUpperInvariant()))
            return Reject(payload, "X-Signature did not match the computed HMAC.");

        return true;
    }

    private bool Reject(WebhookPayload payload, string reason)
    {
        _logger.LogWarning(
            "[WebhookSignatureVerifier] Rejected webhook {Id} from {Provider} — {Reason}",
            payload.WebhookEventId, payload.Provider, reason);
        return false;
    }

    private static bool FixedTimeEqualsExact(string a, string b)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
}
