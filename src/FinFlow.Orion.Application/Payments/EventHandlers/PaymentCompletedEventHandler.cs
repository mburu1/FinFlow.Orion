using FinFlow.Orion.Domain.Events.Payments;
using FinFlow.Orion.Ledger.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.EventHandlers;

public sealed class PaymentCompletedEventHandler
    : INotificationHandler<PaymentCompletedEvent>
{
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(
        ILedgerService ledgerService,
        ILogger<PaymentCompletedEventHandler> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task Handle(PaymentCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] PaymentCompleted — Id: {Id} | Ref: {Ref} | ProviderTxId: {TxId}",
            notification.PaymentId,
            notification.Reference,
            notification.ProviderTransactionId);

        var (debitAccountCode, creditAccountCode) = LedgerAccountResolver.ResolveForProvider(notification.Provider);

        await _ledgerService.PostPaymentAsync(
            notification.Reference,
            notification.Amount,
            debitAccountCode,
            creditAccountCode,
            postedBy: "system.PaymentCompletedEventHandler",
            cancellationToken);
    }
}
