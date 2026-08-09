using FinFlow.Orion.Application.Sagas;
using FinFlow.Orion.Contracts.Payments.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.Consumers;

/// <summary>
/// Starts a durable saga record for every initiated payment, so HandleFailureAsync
/// (triggered by PaymentFailedIntegrationEventConsumer) always has state to build on.
/// </summary>
public sealed class PaymentInitiatedIntegrationEventConsumer : IConsumer<PaymentInitiatedIntegrationEvent>
{
    private readonly IPaymentSagaOrchestrator _saga;
    private readonly ILogger<PaymentInitiatedIntegrationEventConsumer> _logger;

    public PaymentInitiatedIntegrationEventConsumer(
        IPaymentSagaOrchestrator saga,
        ILogger<PaymentInitiatedIntegrationEventConsumer> logger)
    {
        _saga = saga;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentInitiatedIntegrationEvent> context)
    {
        _logger.LogDebug(
            "[PaymentInitiatedIntegrationEventConsumer] Starting saga for payment {PaymentId}",
            context.Message.PaymentId);

        await _saga.StartAsync(context.Message.PaymentId, context.CancellationToken);
    }
}
