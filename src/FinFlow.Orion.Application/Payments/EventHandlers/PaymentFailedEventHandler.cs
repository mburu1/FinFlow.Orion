using FinFlow.Orion.Domain.Events.Payments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.EventHandlers;

public sealed class PaymentFailedEventHandler
    : INotificationHandler<PaymentFailedEvent>
{
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(ILogger<PaymentFailedEventHandler> logger)
        => _logger = logger;

    public Task Handle(PaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "[DomainEvent] PaymentFailed — Id: {Id} | Ref: {Ref} | Reason: {Reason}",
            notification.PaymentId,
            notification.Reference,
            notification.FailureReason);

        return Task.CompletedTask;
    }
}