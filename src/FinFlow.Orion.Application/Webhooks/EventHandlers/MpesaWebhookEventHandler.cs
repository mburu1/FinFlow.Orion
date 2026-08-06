using FinFlow.Orion.Domain.Events.Webhooks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Webhooks.EventHandlers;

public sealed class MpesaWebhookEventHandler
    : INotificationHandler<WebhookReceivedEvent>
{
    private readonly ILogger<MpesaWebhookEventHandler> _logger;

    public MpesaWebhookEventHandler(ILogger<MpesaWebhookEventHandler> logger)
        => _logger = logger;

    public Task Handle(WebhookReceivedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Provider != Domain.Enums.PaymentProvider.MPesa)
            return Task.CompletedTask;

        _logger.LogInformation(
            "[Webhook:MPesa] Received {EventType} — WebhookId: {Id} | PaymentRef: {Ref}",
            notification.EventType,
            notification.WebhookEventId,
            notification.PaymentReference ?? "N/A");

        // MPesa-specific processing logic goes here
        // e.g. parse STK Push callback, update payment status

        return Task.CompletedTask;
    }
}