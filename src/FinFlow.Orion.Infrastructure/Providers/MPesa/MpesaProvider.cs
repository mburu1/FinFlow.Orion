using FinFlow.Orion.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace FinFlow.Orion.Infrastructure.Providers.MPesa;

public sealed class MpesaProvider : IMpesaProvider
{
    private readonly IMpesaClient _client;
    private readonly MpesaConfiguration _config;
    private readonly ILogger<MpesaProvider> _logger;

    public MpesaProvider(
        IMpesaClient client,
        IOptions<MpesaConfiguration> config,
        ILogger<MpesaProvider> logger)
    {
        _client = client;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<MpesaPaymentResult> InitiateStkPushAsync(
        PhoneNumber phoneNumber,
        Money amount,
        string accountReference,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var password = GeneratePassword(timestamp);

            var request = new MpesaStkPushRequest
            {
                BusinessShortCode = _config.BusinessShortCode,
                Password = password,
                Timestamp = timestamp,
                Amount = Math.Ceiling(amount.Amount),   // M-Pesa requires whole numbers
                PartyA = phoneNumber.Number,
                PartyB = _config.BusinessShortCode,
                PhoneNumber = phoneNumber.Number,
                CallBackUrl = _config.CallbackUrl,
                AccountReference = accountReference,
                TransactionDesc = _config.TransactionDesc
            };

            var response = await _client.InitiateStkPushAsync(
                request, $"Bearer {token}");

            _logger.LogInformation(
                "[MPesa] STK Push initiated — CheckoutId: {CheckoutId} | Phone: {Phone} | Amount: {Amount}",
                response.CheckoutRequestId, phoneNumber.Number, amount);

            return new MpesaPaymentResult
            {
                IsSuccessful = response.IsSuccessful,
                CheckoutRequestId = response.CheckoutRequestId,
                MerchantRequestId = response.MerchantRequestId,
                ResponseCode = response.ResponseCode,
                ResponseDescription = response.ResponseDescription,
                CustomerMessage = response.CustomerMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MPesa] STK Push failed — Phone: {Phone} | Amount: {Amount}",
                phoneNumber.Number, amount);

            return new MpesaPaymentResult
            {
                IsSuccessful = false,
                ResponseDescription = ex.Message
            };
        }
    }

    public async Task<MpesaPaymentResult> QueryTransactionStatusAsync(
        string checkoutRequestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var password = GeneratePassword(timestamp);

            var request = new MpesaStkQueryRequest
            {
                BusinessShortCode = _config.BusinessShortCode,
                Password = password,
                Timestamp = timestamp,
                CheckoutRequestId = checkoutRequestId
            };

            var response = await _client.QueryStkStatusAsync(request, $"Bearer {token}");

            _logger.LogInformation(
                "[MPesa] Query — CheckoutId: {Id} | ResultCode: {Code} | Desc: {Desc}",
                checkoutRequestId, response.ResultCode, response.ResultDesc);

            return new MpesaPaymentResult
            {
                IsSuccessful = response.IsSuccessful,
                CheckoutRequestId = response.CheckoutRequestId,
                MerchantRequestId = response.MerchantRequestId,
                ResponseCode = response.ResultCode,
                ResponseDescription = response.ResultDesc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MPesa] Query failed — CheckoutId: {Id}", checkoutRequestId);

            return new MpesaPaymentResult
            {
                IsSuccessful = false,
                ResponseDescription = ex.Message
            };
        }
    }

    public async Task<MpesaReversalResult> ReverseTransactionAsync(
        string transactionId,
        Money amount,
        string remarks,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAccessTokenAsync();

            var request = new MpesaReversalRequest
            {
                TransactionId = transactionId,
                Amount = amount.Amount,
                ReceiverParty = _config.BusinessShortCode,
                ResultUrl = _config.CallbackUrl,
                QueueTimeoutUrl = _config.CallbackUrl,
                Remarks = remarks,
                Occasion = "Reversal"
            };

            var response = await _client.ReverseTransactionAsync(request, $"Bearer {token}");

            _logger.LogInformation(
                "[MPesa] Reversal — TxId: {TxId} | ConversationId: {ConvId} | Code: {Code}",
                transactionId, response.ConversationId, response.ResponseCode);

            return new MpesaReversalResult
            {
                IsSuccessful = response.IsSuccessful,
                ConversationId = response.ConversationId,
                ResponseCode = response.ResponseCode,
                ResponseDescription = response.ResponseDescription
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MPesa] Reversal failed — TxId: {TxId}", transactionId);

            return new MpesaReversalResult
            {
                IsSuccessful = false,
                ResponseDescription = ex.Message
            };
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync()
    {
        var response = await _client.GetAccessTokenAsync();
        return response.AccessToken;
    }

    /// <summary>
    /// M-Pesa password = Base64(ShortCode + PassKey + Timestamp)
    /// </summary>
    private string GeneratePassword(string timestamp)
    {
        var raw = $"{_config.BusinessShortCode}{_config.PassKey}{timestamp}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }
}