using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FinFlow.Orion.Infrastructure.Providers.Bank;
using FinFlow.Orion.Infrastructure.Providers.Card;
using FinFlow.Orion.Infrastructure.Providers.MPesa;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Infrastructure.Providers;

/// <summary>
/// Routes a payment to its provider client and normalizes the result into a
/// ProviderDispatchOutcome the Application layer can act on. Never throws for
/// expected provider failures — those become a failed outcome; unexpected
/// exceptions are still caught defensively so a payment always ends up either
/// authorized or failed, never stuck.
/// </summary>
public sealed class PaymentProviderDispatcher : IPaymentProviderDispatcher
{
    private readonly IMpesaProvider _mpesaProvider;
    private readonly ICardProvider _cardProvider;
    private readonly IBankProvider _bankProvider;
    private readonly ILogger<PaymentProviderDispatcher> _logger;

    public PaymentProviderDispatcher(
        IMpesaProvider mpesaProvider,
        ICardProvider cardProvider,
        IBankProvider bankProvider,
        ILogger<PaymentProviderDispatcher> logger)
    {
        _mpesaProvider = mpesaProvider;
        _cardProvider = cardProvider;
        _bankProvider = bankProvider;
        _logger = logger;
    }

    public async Task<ProviderDispatchOutcome> DispatchAsync(
        Payment payment,
        BankTransferDetails? bankTransferDetails = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return payment.Provider switch
            {
                PaymentProvider.MPesa => await DispatchMpesaAsync(payment, cancellationToken),
                PaymentProvider.Card => await DispatchCardAsync(payment, cancellationToken),
                PaymentProvider.BankTransfer => await DispatchBankAsync(payment, bankTransferDetails, cancellationToken),
                _ => Failed(payment, $"No dispatcher is implemented for provider '{payment.Provider}'.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentProviderDispatcher] Unexpected error dispatching payment {Reference} via {Provider}",
                payment.Reference.Reference, payment.Provider);

            return Failed(payment, $"Unexpected dispatcher error: {ex.Message}");
        }
    }

    private async Task<ProviderDispatchOutcome> DispatchMpesaAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.PhoneNumber is null)
            return Failed(payment, "A phone number is required to dispatch a payment via M-Pesa.");

        var result = await _mpesaProvider.InitiateStkPushAsync(
            payment.PhoneNumber,
            payment.Amount,
            accountReference: payment.Reference.Reference,
            idempotencyKey: payment.IdempotencyKey.Value,
            cancellationToken);

        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.CheckoutRequestId))
        {
            var response = new ProviderResponse(
                result.CheckoutRequestId,
                "PENDING",
                result.CustomerMessage);

            return new ProviderDispatchOutcome(IsAuthorized: true, IsCaptured: false, response);
        }

        return Failed(payment, result.ResponseDescription ?? "M-Pesa STK push failed.");
    }

    private async Task<ProviderDispatchOutcome> DispatchCardAsync(Payment payment, CancellationToken cancellationToken)
    {
        var authResult = await _cardProvider.AuthorizeAsync(
            new CardPaymentRequest
            {
                Amount = payment.Amount,
                Currency = payment.Amount.CurrencyCode,
                CustomerId = payment.CustomerId ?? string.Empty,
                IdempotencyKey = payment.IdempotencyKey.Value,
                Description = payment.Description
            },
            cancellationToken);

        if (!authResult.IsSuccessful || authResult.TransactionId is null)
            return Failed(payment, authResult.FailureMessage ?? "Card authorization failed.");

        var captureResult = await _cardProvider.CaptureAsync(
            authResult.TransactionId,
            payment.Amount,
            cancellationToken);

        if (!captureResult.IsSuccessful || captureResult.TransactionId is null)
        {
            return new ProviderDispatchOutcome(
                IsAuthorized: true,
                IsCaptured: false,
                new ProviderResponse(authResult.TransactionId, "AUTHORIZED", authResult.Status),
                FailureReason: captureResult.FailureMessage ?? "Card capture failed.");
        }

        var response = new ProviderResponse(captureResult.TransactionId, "SUCCESS", captureResult.Status);
        return new ProviderDispatchOutcome(IsAuthorized: true, IsCaptured: true, response);
    }

    private async Task<ProviderDispatchOutcome> DispatchBankAsync(
        Payment payment,
        BankTransferDetails? bankTransferDetails,
        CancellationToken cancellationToken)
    {
        if (bankTransferDetails is null)
            return Failed(payment, "Bank account details are required to dispatch a payment via BankTransfer.");

        var result = await _bankProvider.InitiateTransferAsync(
            new BankTransferRequest
            {
                Amount = payment.Amount,
                AccountNumber = bankTransferDetails.AccountNumber,
                BankCode = bankTransferDetails.BankCode,
                AccountName = bankTransferDetails.AccountName,
                Narration = payment.Description ?? payment.Reference.Reference,
                IdempotencyKey = payment.IdempotencyKey.Value,
                CustomerId = payment.CustomerId
            },
            cancellationToken);

        if (result.IsSuccessful && result.TransactionId is not null)
        {
            var response = new ProviderResponse(
                result.TransactionId,
                "PENDING",
                result.BankReference);

            return new ProviderDispatchOutcome(IsAuthorized: true, IsCaptured: false, response);
        }

        return Failed(payment, result.FailureMessage ?? "Bank transfer initiation failed.");
    }

    private static ProviderDispatchOutcome Failed(Payment payment, string reason)
        => new(
            IsAuthorized: false,
            IsCaptured: false,
            new ProviderResponse($"{payment.Provider}-FAILED-{payment.Reference.Reference}", "FAILED", reason),
            FailureReason: reason);
}
