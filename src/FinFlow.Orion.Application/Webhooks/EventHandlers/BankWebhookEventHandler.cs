using FinFlow.Orion.Domain.Events.Webhooks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Webhooks.EventHandlers;

public sealed class BankWebhookEventHandler
    : INotificationHandler<WebhookReceivedEvent>
{
    private readonly ILogger<BankWebhookEventHandler> _logger;

    public BankWebhookEventHandler(ILogger<BankWebhookEventHandler> logger)
        => _logger = logger;

    public Task Handle(WebhookReceivedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Provider != Domain.Enums.PaymentProvider.BankTransfer)
            return Task.CompletedTask;

        _logger.LogInformation(
            "[Webhook:Bank] Received {EventType} — WebhookId: {Id} | PaymentRef: {Ref}",
            notification.EventType,
            notification.WebhookEventId,
            notification.PaymentReference ?? "N/A");

        // Bank transfer specific processing logic goes here
        // e.g. parse EFT / RTGS settlement callbacks

        return Task.CompletedTask;
    }
}