using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Payments.Commands.RetryPayment;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Commands;

public class RetryPaymentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIdempotencyService _idempotencyService = Substitute.For<IIdempotencyService>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IPaymentProviderDispatcher _dispatcher = Substitute.For<IPaymentProviderDispatcher>();
    private readonly ILogger<RetryPaymentCommandHandler> _logger = Substitute.For<ILogger<RetryPaymentCommandHandler>>();

    public RetryPaymentCommandHandlerTests()
    {
        // NSubstitute does not reliably default an unconfigured Task<string?> to a
        // null-valued task, so pin the non-duplicate-key default explicitly.
        _idempotencyService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
    }

    private RetryPaymentCommandHandler CreateHandler()
        => new(_unitOfWork, _idempotencyService, _paymentRepository, _dispatcher, _logger);

    private static Payment CreateFailedPayment(PaymentProvider provider = PaymentProvider.MPesa)
    {
        var payment = Payment.Create(
            new Money(500, "KES"),
            provider,
            PaymentChannel.Web,
            new IdempotencyKey(new string('a', 20)),
            customerId: "cust-1",
            phoneNumber: provider == PaymentProvider.MPesa ? new PhoneNumber("254712345678") : null);

        payment.MarkAsFailed(new ProviderResponse("TX-FAIL", "FAILED"));
        return payment;
    }

    [Fact]
    public async Task Handle_PaymentNotFailed_ThrowsInvalidPaymentException()
    {
        var payment = Payment.Create(
            new Money(500, "KES"), PaymentProvider.Card, PaymentChannel.Web,
            new IdempotencyKey(new string('a', 20)));

        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RetryPaymentCommand(payment.Id, new string('b', 20)), CancellationToken.None);

        await act.Should().ThrowAsync<FinFlow.Orion.Domain.Exceptions.InvalidPaymentException>();
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsNotFoundException()
    {
        _paymentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RetryPaymentCommand(Guid.NewGuid(), new string('b', 20)), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_FailedPayment_ResetsAndRedispatches()
    {
        var payment = CreateFailedPayment();
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _dispatcher.DispatchAsync(Arg.Any<Payment>(), Arg.Any<BankTransferDetails?>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderDispatchOutcome(true, true, new ProviderResponse("CARD-TX-1", "SUCCESS")));

        var handler = CreateHandler();
        var response = await handler.Handle(
            new RetryPaymentCommand(payment.Id, new string('b', 20), OverrideProvider: "Card"),
            CancellationToken.None);

        response.Status.Should().Be("Captured");
        response.Provider.Should().Be("Card");
        payment.Provider.Should().Be(PaymentProvider.Card);
    }

    [Fact]
    public async Task Handle_UnsupportedOverrideProvider_ThrowsNotFoundException()
    {
        var payment = CreateFailedPayment();
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RetryPaymentCommand(payment.Id, new string('b', 20), OverrideProvider: "NotARealProvider"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ThrowsIdempotencyViolationException()
    {
        _idempotencyService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("cached");

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new RetryPaymentCommand(Guid.NewGuid(), new string('b', 20)), CancellationToken.None);

        await act.Should().ThrowAsync<Domain.Exceptions.IdempotencyViolationException>();
    }
}
