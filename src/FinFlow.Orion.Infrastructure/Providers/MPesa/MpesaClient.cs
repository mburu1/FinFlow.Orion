using Refit;
using System.Text.Json.Serialization;

namespace FinFlow.Orion.Infrastructure.Providers.MPesa;

// ── Refit interface — auto-generates the HTTP client ────────────────────────

[Headers("Content-Type: application/json")]
public interface IMpesaClient
{
    [Post("/oauth/v1/generate?grant_type=client_credentials")]
    Task<MpesaTokenResponse> GetAccessTokenAsync();

    [Post("/mpesa/stkpush/v1/processrequest")]
    Task<MpesaStkPushResponse> InitiateStkPushAsync(
        [Body] MpesaStkPushRequest request,
        [Header("Authorization")] string authorization);

    [Post("/mpesa/stkpushquery/v1/query")]
    Task<MpesaStkQueryResponse> QueryStkStatusAsync(
        [Body] MpesaStkQueryRequest request,
        [Header("Authorization")] string authorization);

    [Post("/mpesa/reversal/v1/request")]
    Task<MpesaReversalResponse> ReverseTransactionAsync(
        [Body] MpesaReversalRequest request,
        [Header("Authorization")] string authorization);
}

// ── Token ────────────────────────────────────────────────────────────────────

public sealed class MpesaTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public string ExpiresIn { get; init; } = string.Empty;
}

// ── STK Push Request ─────────────────────────────────────────────────────────

public sealed class MpesaStkPushRequest
{
    [JsonPropertyName("BusinessShortCode")]
    public string BusinessShortCode { get; init; } = string.Empty;

    [JsonPropertyName("Password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("TransactionType")]
    public string TransactionType { get; init; } = "CustomerPayBillOnline";

    [JsonPropertyName("Amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("PartyA")]
    public string PartyA { get; init; } = string.Empty;       // Phone number

    [JsonPropertyName("PartyB")]
    public string PartyB { get; init; } = string.Empty;       // Short code

    [JsonPropertyName("PhoneNumber")]
    public string PhoneNumber { get; init; } = string.Empty;

    [JsonPropertyName("CallBackURL")]
    public string CallBackUrl { get; init; } = string.Empty;

    [JsonPropertyName("AccountReference")]
    public string AccountReference { get; init; } = string.Empty;

    [JsonPropertyName("TransactionDesc")]
    public string TransactionDesc { get; init; } = string.Empty;
}

// ── STK Push Response ────────────────────────────────────────────────────────

public sealed class MpesaStkPushResponse
{
    [JsonPropertyName("MerchantRequestID")]
    public string MerchantRequestId { get; init; } = string.Empty;

    [JsonPropertyName("CheckoutRequestID")]
    public string CheckoutRequestId { get; init; } = string.Empty;

    [JsonPropertyName("ResponseCode")]
    public string ResponseCode { get; init; } = string.Empty;

    [JsonPropertyName("ResponseDescription")]
    public string ResponseDescription { get; init; } = string.Empty;

    [JsonPropertyName("CustomerMessage")]
    public string CustomerMessage { get; init; } = string.Empty;

    public bool IsSuccessful => ResponseCode == "0";
}

// ── STK Query ────────────────────────────────────────────────────────────────

public sealed class MpesaStkQueryRequest
{
    [JsonPropertyName("BusinessShortCode")]
    public string BusinessShortCode { get; init; } = string.Empty;

    [JsonPropertyName("Password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("CheckoutRequestID")]
    public string CheckoutRequestId { get; init; } = string.Empty;
}

public sealed class MpesaStkQueryResponse
{
    [JsonPropertyName("ResponseCode")]
    public string ResponseCode { get; init; } = string.Empty;

    [JsonPropertyName("ResponseDescription")]
    public string ResponseDescription { get; init; } = string.Empty;

    [JsonPropertyName("MerchantRequestID")]
    public string MerchantRequestId { get; init; } = string.Empty;

    [JsonPropertyName("CheckoutRequestID")]
    public string CheckoutRequestId { get; init; } = string.Empty;

    [JsonPropertyName("ResultCode")]
    public string ResultCode { get; init; } = string.Empty;

    [JsonPropertyName("ResultDesc")]
    public string ResultDesc { get; init; } = string.Empty;

    public bool IsSuccessful => ResultCode == "0";
}

// ── Reversal ─────────────────────────────────────────────────────────────────

public sealed class MpesaReversalRequest
{
    [JsonPropertyName("Initiator")]
    public string Initiator { get; init; } = string.Empty;

    [JsonPropertyName("SecurityCredential")]
    public string SecurityCredential { get; init; } = string.Empty;

    [JsonPropertyName("CommandID")]
    public string CommandId { get; init; } = "TransactionReversal";

    [JsonPropertyName("TransactionID")]
    public string TransactionId { get; init; } = string.Empty;

    [JsonPropertyName("Amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("ReceiverParty")]
    public string ReceiverParty { get; init; } = string.Empty;

    [JsonPropertyName("RecieverIdentifierType")]
    public string ReceiverIdentifierType { get; init; } = "4";

    [JsonPropertyName("ResultURL")]
    public string ResultUrl { get; init; } = string.Empty;

    [JsonPropertyName("QueueTimeOutURL")]
    public string QueueTimeoutUrl { get; init; } = string.Empty;

    [JsonPropertyName("Remarks")]
    public string Remarks { get; init; } = string.Empty;

    [JsonPropertyName("Occasion")]
    public string Occasion { get; init; } = string.Empty;
}

public sealed class MpesaReversalResponse
{
    [JsonPropertyName("ConversationID")]
    public string ConversationId { get; init; } = string.Empty;

    [JsonPropertyName("OriginatorConversationID")]
    public string OriginatorConversationId { get; init; } = string.Empty;

    [JsonPropertyName("ResponseCode")]
    public string ResponseCode { get; init; } = string.Empty;

    [JsonPropertyName("ResponseDescription")]
    public string ResponseDescription { get; init; } = string.Empty;

    public bool IsSuccessful => ResponseCode == "0";
}