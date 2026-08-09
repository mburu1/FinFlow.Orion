using FinFlow.Orion.Application.Payments.Consumers;
using FinFlow.Orion.Application.Sagas;
using FinFlow.Orion.Contracts.Payments.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Consumers;

public class PaymentInitiatedIntegrationEventConsumerTests
{
    [Fact]
    public async Task Consume_CallsSagaStartAsync_WithMessagePaymentId()
    {
        var saga = Substitute.For<IPaymentSagaOrchestrator>();
        var logger = Substitute.For<ILogger<PaymentInitiatedIntegrationEventConsumer>>();
        var consumer = new PaymentInitiatedIntegrationEventConsumer(saga, logger);

        var message = new PaymentInitiatedIntegrationEvent { PaymentId = Guid.NewGuid() };
        var context = Substitute.For<ConsumeContext<PaymentInitiatedIntegrationEvent>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await saga.Received(1).StartAsync(message.PaymentId, Arg.Any<CancellationToken>());
    }
}
