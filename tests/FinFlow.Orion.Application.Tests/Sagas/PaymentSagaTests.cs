using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Payments.Commands.RetryPayment;
using FinFlow.Orion.Application.Sagas;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Sagas;

public class PaymentSagaTests
{
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IPaymentSagaStateRepository _sagaStateRepository = Substitute.For<IPaymentSagaStateRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<PaymentSaga> _logger = Substitute.For<ILogger<PaymentSaga>>();

    private PaymentSaga CreateSaga() => new(_paymentRepository, _sagaStateRepository, _mediator, _logger);

    private static Payment CreatePayment(PaymentProvider provider)
        => Payment.Create(
            new Money(100, "KES"), provider, PaymentChannel.Web,
            new IdempotencyKey(new string('a', 20)));

    [Fact]
    public async Task StartAsync_NoExistingState_CreatesAndPersistsState()
    {
        var payment = CreatePayment(PaymentProvider.MPesa);
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _sagaStateRepository.GetActiveByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns((PaymentSagaState?)null);

        await CreateSaga().StartAsync(payment.Id);

        await _sagaStateRepository.Received(1).AddAsync(
            Arg.Is<PaymentSagaState>(s => s.PaymentId == payment.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ActiveStateAlreadyExists_DoesNotCreateAnother()
    {
        var payment = CreatePayment(PaymentProvider.MPesa);
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _sagaStateRepository.GetActiveByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(new PaymentSagaState { PaymentId = payment.Id });

        await CreateSaga().StartAsync(payment.Id);

        await _sagaStateRepository.DidNotReceive().AddAsync(Arg.Any<PaymentSagaState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleFailureAsync_MpesaFailure_SendsRetryWithCardOverride()
    {
        var payment = CreatePayment(PaymentProvider.MPesa);
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _sagaStateRepository.GetActiveByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(new PaymentSagaState { PaymentId = payment.Id, CurrentStep = "PaymentInitiated" });

        await CreateSaga().HandleFailureAsync(payment.Id, "stk push failed");

        await _mediator.Received(1).Send(
            Arg.Is<RetryPaymentCommand>(c => c.PaymentId == payment.Id && c.OverrideProvider == "Card"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleFailureAsync_CardFailure_SendsRetryWithBankTransferOverride()
    {
        var payment = CreatePayment(PaymentProvider.Card);
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _sagaStateRepository.GetActiveByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(new PaymentSagaState { PaymentId = payment.Id, CurrentStep = "FallbackTo:Card" });

        await CreateSaga().HandleFailureAsync(payment.Id, "card declined");

        await _mediator.Received(1).Send(
            Arg.Is<RetryPaymentCommand>(c => c.OverrideProvider == "BankTransfer"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleFailureAsync_BankTransferFailure_ChainExhausted_CompensatesWithoutRetry()
    {
        var payment = CreatePayment(PaymentProvider.BankTransfer);
        var state = new PaymentSagaState { PaymentId = payment.Id, CurrentStep = "FallbackTo:BankTransfer" };
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _sagaStateRepository.GetActiveByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(state);

        await CreateSaga().HandleFailureAsync(payment.Id, "bank transfer failed");

        await _mediator.DidNotReceive().Send(Arg.Any<RetryPaymentCommand>(), Arg.Any<CancellationToken>());
        state.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task HandleFailureAsync_RetryCountExhausted_CompensatesEvenWithFallbackAvailable()
    {
        var payment = CreatePayment(PaymentProvider.MPesa);
        var state = new PaymentSagaState
        {
            PaymentId = payment.Id,
            CurrentStep = "PaymentInitiated",
            RetryCount = 3,
            MaxRetries = 3
        };
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _sagaStateRepository.GetActiveByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(state);

        await CreateSaga().HandleFailureAsync(payment.Id, "stk push failed again");

        await _mediator.DidNotReceive().Send(Arg.Any<RetryPaymentCommand>(), Arg.Any<CancellationToken>());
        state.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CompensateAsync_MarksStateCompleted()
    {
        var payment = CreatePayment(PaymentProvider.BankTransfer);
        var state = new PaymentSagaState { PaymentId = payment.Id };
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _sagaStateRepository.GetActiveByPaymentIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(state);

        await CreateSaga().CompensateAsync(payment.Id);

        state.IsCompleted.Should().BeTrue();
        state.CompletedAt.Should().NotBeNull();
    }
}
