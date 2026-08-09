using FinFlow.Orion.Domain.Events.Payments;
using FinFlow.Orion.Ledger.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.EventHandlers;

public sealed class PaymentReversedEventHandler
    : INotificationHandler<PaymentReversedEvent>
{
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<PaymentReversedEventHandler> _logger;

    public PaymentReversedEventHandler(
        ILedgerService ledgerService,
        ILogger<PaymentReversedEventHandler> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task Handle(PaymentReversedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] PaymentReversed — Id: {Id} | Ref: {Ref} | Reason: {Reason}",
            notification.PaymentId,
            notification.Reference,
            notification.Reason);

        var (debitAccountCode, creditAccountCode) = LedgerAccountResolver.ResolveForProvider(notification.Provider);

        await _ledgerService.PostReversalAsync(
            notification.Reference,
            notification.Amount,
            debitAccountCode,
            creditAccountCode,
            postedBy: "system.PaymentReversedEventHandler",
            reason: notification.Reason,
            cancellationToken);
    }
}
