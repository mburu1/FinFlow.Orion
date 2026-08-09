using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Payments.Commands.InitiatePayment;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Exceptions;
using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Commands;

public class InitiatePaymentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIdempotencyService _idempotencyService = Substitute.For<IIdempotencyService>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IPaymentProviderDispatcher _dispatcher = Substitute.For<IPaymentProviderDispatcher>();
    private readonly ILogger<InitiatePaymentCommandHandler> _logger = Substitute.For<ILogger<InitiatePaymentCommandHandler>>();

    public InitiatePaymentCommandHandlerTests()
    {
        // NSubstitute does not reliably default an unconfigured Task<string?> to a
        // null-valued task, so pin the non-duplicate-key default explicitly.
        _idempotencyService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
    }

    private InitiatePaymentCommandHandler CreateHandler()
        => new(_unitOfWork, _idempotencyService, _paymentRepository, _dispatcher, _logger);

    private static InitiatePaymentCommand CreateCommand(string provider = "Card", string? phoneNumber = null)
        => new(
            Amount: 1000,
            CurrencyCode: "KES",
            Provider: provider,
            Channel: "Web",
            IdempotencyKey: new string('a', 20),
            CustomerId: "cust-1",
            PhoneNumber: phoneNumber,
            Description: "test payment");

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ThrowsIdempotencyViolationException()
    {
        _idempotencyService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("cached-response");

        var handler = CreateHandler();
        var act = () => handler.Handle(CreateCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyViolationException>();
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CardCapturedOutcome_ResultsInCapturedStatus()
    {
        _dispatcher.DispatchAsync(Arg.Any<Payment>(), Arg.Any<BankTransferDetails?>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderDispatchOutcome(
                IsAuthorized: true,
                IsCaptured: true,
                new ProviderResponse("CARD-TX-1", "SUCCESS")));

        var handler = CreateHandler();
        var response = await handler.Handle(CreateCommand("Card"), CancellationToken.None);

        response.Status.Should().Be("Captured");
        await _paymentRepository.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MpesaAuthorizedOnlyOutcome_ResultsInAuthorizedStatus()
    {
        _dispatcher.DispatchAsync(Arg.Any<Payment>(), Arg.Any<BankTransferDetails?>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderDispatchOutcome(
                IsAuthorized: true,
                IsCaptured: false,
                new ProviderResponse("ws_CO_123", "PENDING")));

        var handler = CreateHandler();
        var response = await handler.Handle(CreateCommand("MPesa", "254712345678"), CancellationToken.None);

        response.Status.Should().Be("Authorized");
    }

    [Fact]
    public async Task Handle_FailedDispatchOutcome_ResultsInFailedStatus_AndDoesNotThrow()
    {
        _dispatcher.DispatchAsync(Arg.Any<Payment>(), Arg.Any<BankTransferDetails?>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderDispatchOutcome(
                IsAuthorized: false,
                IsCaptured: false,
                new ProviderResponse("CARD-FAILED", "FAILED"),
                FailureReason: "card declined"));

        var handler = CreateHandler();
        var response = await handler.Handle(CreateCommand("Card"), CancellationToken.None);

        response.Status.Should().Be("Failed");
    }
}
