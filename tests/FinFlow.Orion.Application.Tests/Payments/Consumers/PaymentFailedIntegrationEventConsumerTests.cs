using FinFlow.Orion.Application.Payments.Consumers;
using FinFlow.Orion.Application.Sagas;
using FinFlow.Orion.Contracts.Payments.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Consumers;

public class PaymentFailedIntegrationEventConsumerTests
{
    [Fact]
    public async Task Consume_CallsSagaHandleFailureAsync_WithMessagePaymentIdAndReason()
    {
        var saga = Substitute.For<IPaymentSagaOrchestrator>();
        var logger = Substitute.For<ILogger<PaymentFailedIntegrationEventConsumer>>();
        var consumer = new PaymentFailedIntegrationEventConsumer(saga, logger);

        var message = new PaymentFailedIntegrationEvent { PaymentId = Guid.NewGuid(), FailureReason = "declined" };
        var context = Substitute.For<ConsumeContext<PaymentFailedIntegrationEvent>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await saga.Received(1).HandleFailureAsync(message.PaymentId, "declined", Arg.Any<CancellationToken>());
    }
}
