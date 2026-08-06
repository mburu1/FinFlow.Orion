using FinFlow.Orion.Domain.Events.Payments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.EventHandlers;

public sealed class PaymentCompletedEventHandler
    : INotificationHandler<PaymentCompletedEvent>
{
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(ILogger<PaymentCompletedEventHandler> logger)
        => _logger = logger;

    public Task Handle(PaymentCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] PaymentCompleted — Id: {Id} | Ref: {Ref} | ProviderTxId: {TxId}",
            notification.PaymentId,
            notification.Reference,
            notification.ProviderTransactionId);

        return Task.CompletedTask;
    }
}