using FinFlow.Orion.Domain.Events.Webhooks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Webhooks.EventHandlers;

public sealed class CardWebhookEventHandler
    : INotificationHandler<WebhookReceivedEvent>
{
    private readonly ILogger<CardWebhookEventHandler> _logger;

    public CardWebhookEventHandler(ILogger<CardWebhookEventHandler> logger)
        => _logger = logger;

    public Task Handle(WebhookReceivedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Provider != Domain.Enums.PaymentProvider.Card)
            return Task.CompletedTask;

        _logger.LogInformation(
            "[Webhook:Card] Received {EventType} — WebhookId: {Id} | PaymentRef: {Ref}",
            notification.EventType,
            notification.WebhookEventId,
            notification.PaymentReference ?? "N/A");

        // Card-specific processing logic goes here
        // e.g. parse authorization / capture callbacks

        return Task.CompletedTask;
    }
}