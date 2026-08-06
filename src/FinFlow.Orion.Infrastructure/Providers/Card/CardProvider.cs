using FinFlow.Orion.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Infrastructure.Providers.Card;

/// <summary>
/// Generic card payment provider.
/// Swap the underlying client (Stripe, Flutterwave, Paystack)
/// without changing the interface contract.
/// </summary>
public sealed class CardProvider : ICardProvider
{
    private readonly ILogger<CardProvider> _logger;

    public CardProvider(ILogger<CardProvider> logger)
        => _logger = logger;

    public async Task<CardPaymentResult> AuthorizeAsync(
        CardPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Card] Authorizing — CustomerId: {Id} | Amount: {Amount} | Key: {Key}",
                request.CustomerId, request.Amount, request.IdempotencyKey);

            // TODO: Inject and call the specific card gateway client here
            // e.g. Stripe: await _stripeClient.PaymentIntents.CreateAsync(...)
            // e.g. Flutterwave: await _flutterwaveClient.ChargeAsync(...)

            await Task.Delay(0, cancellationToken); // placeholder

            return new CardPaymentResult
            {
                IsSuccessful = true,
                TransactionId = $"CARD-{Guid.NewGuid():N}",
                Status = "authorized"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Card] Authorization failed — CustomerId: {Id}", request.CustomerId);

            return new CardPaymentResult
            {
                IsSuccessful = false,
                FailureMessage = ex.Message
            };
        }
    }

    public async Task<CardPaymentResult> CaptureAsync(
        string providerTransactionId,
        Money amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Card] Capturing — TxId: {Id} | Amount: {Amount}",
                providerTransactionId, amount);

            await Task.Delay(0, cancellationToken);

            return new CardPaymentResult
            {
                IsSuccessful = true,
                TransactionId = providerTransactionId,
                Status = "captured"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Card] Capture failed — TxId: {Id}", providerTransactionId);

            return new CardPaymentResult
            {
                IsSuccessful = false,
                FailureMessage = ex.Message
            };
        }
    }

    public async Task<CardPaymentResult> RefundAsync(
        string providerTransactionId,
        Money amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Card] Refunding — TxId: {Id} | Amount: {Amount} | Reason: {Reason}",
                providerTransactionId, amount, reason);

            await Task.Delay(0, cancellationToken);

            return new CardPaymentResult
            {
                IsSuccessful = true,
                TransactionId = providerTransactionId,
                Status = "refunded"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Card] Refund failed — TxId: {Id}", providerTransactionId);

            return new CardPaymentResult
            {
                IsSuccessful = false,
                FailureMessage = ex.Message
            };
        }
    }
}