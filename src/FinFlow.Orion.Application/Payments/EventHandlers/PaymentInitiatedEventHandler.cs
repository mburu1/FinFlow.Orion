using FinFlow.Orion.Domain.Events.Payments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.EventHandlers;

public sealed class PaymentInitiatedEventHandler
    : INotificationHandler<PaymentInitiatedEvent>
{
    private readonly ILogger<PaymentInitiatedEventHandler> _logger;

    public PaymentInitiatedEventHandler(ILogger<PaymentInitiatedEventHandler> logger)
        => _logger = logger;

    public Task Handle(PaymentInitiatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] PaymentInitiated — Id: {Id} | Ref: {Ref} | Provider: {Provider} | Amount: {Amount}",
            notification.PaymentId,
            notification.Reference,
            notification.Provider,
            notification.Amount);

        return Task.CompletedTask;
    }
}