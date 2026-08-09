using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments;

/// <summary>
/// Shared dispatch → status-transition → attempt-recording sequence used by both
/// InitiatePaymentCommandHandler and RetryPaymentCommandHandler, so the two stay
/// in lockstep instead of duplicating this logic.
/// </summary>
public static class PaymentDispatchProcessor
{
    public static async Task<ProviderDispatchOutcome> DispatchAndTransitionAsync(
        Payment payment,
        IPaymentProviderDispatcher dispatcher,
        int attemptNumber,
        ILogger logger,
        BankTransferDetails? bankTransferDetails = null,
        CancellationToken cancellationToken = default)
    {
        ProviderDispatchOutcome outcome;
        try
        {
            outcome = await dispatcher.DispatchAsync(payment, bankTransferDetails, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[PaymentDispatchProcessor] Dispatcher threw unexpectedly for payment {Reference} via {Provider}",
                payment.Reference.Reference, payment.Provider);

            outcome = new ProviderDispatchOutcome(
                IsAuthorized: false,
                IsCaptured: false,
                new ProviderResponse($"{payment.Provider}-ERROR-{payment.Reference.Reference}", "FAILED", ex.Message),
                FailureReason: ex.Message);
        }

        if (outcome.IsAuthorized)
        {
            payment.MarkAsAuthorized(outcome.Response);
            if (outcome.IsCaptured)
                payment.MarkAsCaptured(outcome.Response);
        }
        else
        {
            payment.MarkAsFailed(outcome.Response);
        }

        var resultStatus = outcome.IsCaptured
            ? PaymentStatus.Captured
            : outcome.IsAuthorized ? PaymentStatus.Authorized : PaymentStatus.Failed;

        payment.AddAttempt(PaymentAttempt.Create(
            payment.Id,
            attemptNumber,
            payment.Provider,
            resultStatus,
            outcome.Response.ProviderTransactionId,
            outcome.FailureReason));

        return outcome;
    }
}
