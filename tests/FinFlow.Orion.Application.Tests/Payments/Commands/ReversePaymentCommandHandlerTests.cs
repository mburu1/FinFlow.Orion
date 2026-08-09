using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Payments.Commands.ReversePayment;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Exceptions;
using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Commands;

public class ReversePaymentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIdempotencyService _idempotencyService = Substitute.For<IIdempotencyService>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ILogger<ReversePaymentCommandHandler> _logger = Substitute.For<ILogger<ReversePaymentCommandHandler>>();

    public ReversePaymentCommandHandlerTests()
    {
        // NSubstitute does not reliably default an unconfigured Task<string?> to a
        // null-valued task, so pin the non-duplicate-key default explicitly.
        _idempotencyService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
    }

    private ReversePaymentCommandHandler CreateHandler()
        => new(_unitOfWork, _idempotencyService, _paymentRepository, _logger);

    private static Payment CreateCapturedPayment()
    {
        var payment = Payment.Create(
            new Money(500, "KES"), PaymentProvider.Card, PaymentChannel.Web,
            new IdempotencyKey(new string('a', 20)));
        payment.MarkAsAuthorized(new ProviderResponse("TX-1", "PENDING"));
        payment.MarkAsCaptured(new ProviderResponse("TX-1", "SUCCESS"));
        return payment;
    }

    [Fact]
    public async Task Handle_CapturedPayment_ReversesAndReturnsTrue()
    {
        var payment = CreateCapturedPayment();
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ReversePaymentCommand(payment.Id, "customer request", "admin", new string('b', 20)),
            CancellationToken.None);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Reversed);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsNotFoundException()
    {
        _paymentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ReversePaymentCommand(Guid.NewGuid(), "reason", "admin", new string('b', 20)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PaymentNotCaptured_ThrowsInvalidPaymentException()
    {
        var payment = Payment.Create(
            new Money(500, "KES"), PaymentProvider.Card, PaymentChannel.Web,
            new IdempotencyKey(new string('a', 20)));
        _paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ReversePaymentCommand(payment.Id, "reason", "admin", new string('b', 20)),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPaymentException>();
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ThrowsIdempotencyViolationException()
    {
        _idempotencyService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("cached");

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new ReversePaymentCommand(Guid.NewGuid(), "reason", "admin", new string('b', 20)),
            CancellationToken.None);

        await act.Should().ThrowAsync<Domain.Exceptions.IdempotencyViolationException>();
    }
}
