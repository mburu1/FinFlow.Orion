using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Contracts.Webhooks.Events;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Webhooks.Consumers;

/// <summary>
/// Closes the loop on asynchronous payment providers (M-Pesa, BankTransfer): once a
/// provider's callback has been verified and stored as a WebhookEvent, this consumer
/// resolves the matching Payment and transitions it to Captured/Failed — which in
/// turn cascades into ledger posting via PaymentCompletedEventHandler/
/// PaymentReversedEventHandler.
/// </summary>
public sealed class WebhookReceivedIntegrationEventConsumer : IConsumer<WebhookReceivedIntegrationEvent>
{
    private readonly IWebhookRepository _webhookRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookReceivedIntegrationEventConsumer> _logger;

    public WebhookReceivedIntegrationEventConsumer(
        IWebhookRepository webhookRepository,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        ILogger<WebhookReceivedIntegrationEventConsumer> logger)
    {
        _webhookRepository = webhookRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WebhookReceivedIntegrationEvent> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var webhookEvent = await _webhookRepository.GetByIdAsync(message.WebhookEventId, cancellationToken);
        if (webhookEvent is null)
        {
            _logger.LogWarning(
                "[WebhookReceivedIntegrationEventConsumer] WebhookEvent {Id} not found — skipping.",
                message.WebhookEventId);
            return;
        }

        var payment = webhookEvent.PaymentReference is not null
            ? await _paymentRepository.GetByReferenceAsync(webhookEvent.PaymentReference, cancellationToken)
            : webhookEvent.ProviderTransactionId is not null
                ? await _paymentRepository.GetByProviderTransactionIdAsync(webhookEvent.ProviderTransactionId, cancellationToken)
                : null;

        if (payment is null)
        {
            _logger.LogWarning(
                "[WebhookReceivedIntegrationEventConsumer] No matching payment for webhook {Id} " +
                "(Reference: {Reference}, ProviderTxId: {TxId}).",
                webhookEvent.Id, webhookEvent.PaymentReference, webhookEvent.ProviderTransactionId);

            webhookEvent.MarkAsFailed("No matching payment found for this webhook.");
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken);
            return;
        }

        if (payment.Status == Domain.Enums.PaymentStatus.Authorized)
        {
            var response = new ProviderResponse(
                webhookEvent.ProviderTransactionId ?? payment.Reference.Reference,
                webhookEvent.EventType == WebhookEventType.PaymentCompleted ? "SUCCESS" : "FAILED");

            if (webhookEvent.EventType == WebhookEventType.PaymentCompleted)
                payment.MarkAsCaptured(response);
            else if (webhookEvent.EventType == WebhookEventType.PaymentFailed)
                payment.MarkAsFailed(response);
        }
        else
        {
            _logger.LogDebug(
                "[WebhookReceivedIntegrationEventConsumer] Payment {Reference} is already {Status} — " +
                "webhook {EventType} has no further effect.",
                payment.Reference.Reference, payment.Status, webhookEvent.EventType);
        }

        webhookEvent.MarkAsProcessed();
        await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken);

        // Cascades into PaymentCompletedEventHandler/PaymentReversedEventHandler → ledger posting.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
