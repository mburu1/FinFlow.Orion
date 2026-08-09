using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Webhooks.Consumers;
using FinFlow.Orion.Contracts.Webhooks.Events;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Entities.Webhooks;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Webhooks.Consumers;

public class WebhookReceivedIntegrationEventConsumerTests
{
    private readonly IWebhookRepository _webhookRepository = Substitute.For<IWebhookRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<WebhookReceivedIntegrationEventConsumer> _logger =
        Substitute.For<ILogger<WebhookReceivedIntegrationEventConsumer>>();

    private WebhookReceivedIntegrationEventConsumer CreateConsumer()
        => new(_webhookRepository, _paymentRepository, _unitOfWork, _logger);

    private static ConsumeContext<WebhookReceivedIntegrationEvent> CreateContext(WebhookReceivedIntegrationEvent message)
    {
        var context = Substitute.For<ConsumeContext<WebhookReceivedIntegrationEvent>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_WebhookEventNotFound_DoesNothing()
    {
        var message = new WebhookReceivedIntegrationEvent { WebhookEventId = Guid.NewGuid() };
        _webhookRepository.GetByIdAsync(message.WebhookEventId, Arg.Any<CancellationToken>())
            .Returns((WebhookEvent?)null);

        await CreateConsumer().Consume(CreateContext(message));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_NoMatchingPayment_MarksWebhookEventFailed()
    {
        var webhookEvent = WebhookEvent.Create(
            PaymentProvider.MPesa, WebhookEventType.PaymentCompleted, "{}", providerTransactionId: "ws_CO_UNKNOWN");

        var message = new WebhookReceivedIntegrationEvent { WebhookEventId = webhookEvent.Id };
        _webhookRepository.GetByIdAsync(webhookEvent.Id, Arg.Any<CancellationToken>()).Returns(webhookEvent);
        _paymentRepository.GetByProviderTransactionIdAsync("ws_CO_UNKNOWN", Arg.Any<CancellationToken>())
            .Returns((Payment?)null);

        await CreateConsumer().Consume(CreateContext(message));

        webhookEvent.ProcessingError.Should().NotBeNull();
        await _webhookRepository.Received(1).UpdateAsync(webhookEvent, Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_MatchingAuthorizedPayment_PaymentCompletedEvent_CapturesPayment()
    {
        var payment = Payment.Create(
            new Money(100, "KES"), PaymentProvider.MPesa, PaymentChannel.Mobile,
            new IdempotencyKey(new string('a', 20)), phoneNumber: new PhoneNumber("254712345678"));
        payment.MarkAsAuthorized(new ProviderResponse("ws_CO_123", "PENDING"));

        var webhookEvent = WebhookEvent.Create(
            PaymentProvider.MPesa, WebhookEventType.PaymentCompleted, "{}", providerTransactionId: "ws_CO_123");

        var message = new WebhookReceivedIntegrationEvent { WebhookEventId = webhookEvent.Id };
        _webhookRepository.GetByIdAsync(webhookEvent.Id, Arg.Any<CancellationToken>()).Returns(webhookEvent);
        _paymentRepository.GetByProviderTransactionIdAsync("ws_CO_123", Arg.Any<CancellationToken>()).Returns(payment);

        await CreateConsumer().Consume(CreateContext(message));

        payment.Status.Should().Be(PaymentStatus.Captured);
        webhookEvent.IsProcessed.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_PaymentAlreadyCaptured_WebhookHasNoFurtherEffect()
    {
        var payment = Payment.Create(
            new Money(100, "KES"), PaymentProvider.MPesa, PaymentChannel.Mobile,
            new IdempotencyKey(new string('a', 20)), phoneNumber: new PhoneNumber("254712345678"));
        payment.MarkAsAuthorized(new ProviderResponse("ws_CO_123", "PENDING"));
        payment.MarkAsCaptured(new ProviderResponse("ws_CO_123", "SUCCESS"));

        var webhookEvent = WebhookEvent.Create(
            PaymentProvider.MPesa, WebhookEventType.PaymentCompleted, "{}", providerTransactionId: "ws_CO_123");

        var message = new WebhookReceivedIntegrationEvent { WebhookEventId = webhookEvent.Id };
        _webhookRepository.GetByIdAsync(webhookEvent.Id, Arg.Any<CancellationToken>()).Returns(webhookEvent);
        _paymentRepository.GetByProviderTransactionIdAsync("ws_CO_123", Arg.Any<CancellationToken>()).Returns(payment);

        await CreateConsumer().Consume(CreateContext(message));

        payment.Status.Should().Be(PaymentStatus.Captured);
        webhookEvent.IsProcessed.Should().BeTrue();
    }
}
