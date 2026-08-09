using FinFlow.Orion.Application.Sagas;
using FinFlow.Orion.Contracts.Payments.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.Consumers;

/// <summary>
/// Drives the payment saga's provider fallback chain whenever a payment fails.
/// Runs off the bus (its own scope/DbContext) rather than inline in a domain-event
/// handler, since the saga sends RetryPaymentCommand — which itself calls
/// SaveChangesAsync — and that must never nest inside the SaveChangesAsync call
/// that originally raised PaymentFailedEvent.
/// </summary>
public sealed class PaymentFailedIntegrationEventConsumer : IConsumer<PaymentFailedIntegrationEvent>
{
    private readonly IPaymentSagaOrchestrator _saga;
    private readonly ILogger<PaymentFailedIntegrationEventConsumer> _logger;

    public PaymentFailedIntegrationEventConsumer(
        IPaymentSagaOrchestrator saga,
        ILogger<PaymentFailedIntegrationEventConsumer> logger)
    {
        _saga = saga;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[PaymentFailedIntegrationEventConsumer] Handling failure for payment {PaymentId} — Reason: {Reason}",
            context.Message.PaymentId, context.Message.FailureReason);

        await _saga.HandleFailureAsync(
            context.Message.PaymentId,
            context.Message.FailureReason,
            context.CancellationToken);
    }
}
