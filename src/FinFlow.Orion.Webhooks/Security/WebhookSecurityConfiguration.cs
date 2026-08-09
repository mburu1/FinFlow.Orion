namespace FinFlow.Orion.Webhooks.Security;

/// <summary>
/// Per-provider webhook verification secrets, bound from the "Webhooks:Security"
/// configuration section. Set real values via user-secrets/environment variables —
/// never commit real secrets to appsettings.json.
/// </summary>
public sealed class WebhookSecurityConfiguration
{
    public const string SectionName = "Webhooks:Security";

    public MpesaSecuritySettings MPesa { get; init; } = new();
    public ProviderSecuritySettings Card { get; init; } = new();
    public ProviderSecuritySettings Bank { get; init; } = new();

    public sealed class MpesaSecuritySettings
    {
        /// <summary>
        /// Shared secret expected on the X-MPesa-Secret header. Safaricom Daraja
        /// does not HMAC-sign callbacks, so authenticity is enforced via a secret
        /// embedded in the registered callback URL/header instead. Left empty in
        /// local dev — see WebhookSignatureVerifier for the accept-when-unset behavior.
        /// </summary>
        public string? SharedSecret { get; init; }
    }

    public sealed class ProviderSecuritySettings
    {
        /// <summary>
        /// HMAC-SHA256 signing secret. The provider must send the hex-encoded
        /// HMAC of the raw request body in the X-Signature header.
        /// </summary>
        public string? SigningSecret { get; init; }
    }
}
