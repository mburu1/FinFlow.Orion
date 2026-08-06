using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Contracts.Payments.Responses;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.Commands.InitiatePayment;

public sealed class InitiatePaymentCommandHandler
    : IRequestHandler<InitiatePaymentCommand, InitiatePaymentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<InitiatePaymentCommandHandler> _logger;

    public InitiatePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        IPaymentRepository paymentRepository,
        ILogger<InitiatePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<InitiatePaymentResponse> Handle(
        InitiatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        // Idempotency check — return cached response if duplicate request
        var cached = await _idempotencyService.GetAsync(request.IdempotencyKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogWarning("[Idempotency] Duplicate request detected for key {Key}", request.IdempotencyKey);
            throw new Domain.Exceptions.IdempotencyViolationException(request.IdempotencyKey);
        }

        if (!Enum.TryParse<PaymentProvider>(request.Provider, true, out var provider))
            throw new NotFoundException($"Payment provider '{request.Provider}' is not supported.");

        if (!Enum.TryParse<PaymentChannel>(request.Channel, true, out var channel))
            throw new NotFoundException($"Payment channel '{request.Channel}' is not supported.");

        var money = new Money(request.Amount, request.CurrencyCode);
        var idempotencyKey = new IdempotencyKey(request.IdempotencyKey);

        PhoneNumber? phoneNumber = request.PhoneNumber is not null
            ? new PhoneNumber(request.PhoneNumber)
            : null;

        var payment = Payment.Create(
            amount: money,
            provider: provider,
            channel: channel,
            idempotencyKey: idempotencyKey,
            customerId: request.CustomerId,
            phoneNumber: phoneNumber,
            description: request.Description);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Store idempotency key to prevent replays
        await _idempotencyService.StoreAsync(
            request.IdempotencyKey,
            payment.Id.ToString(),
            TimeSpan.FromHours(24),
            cancellationToken);

        _logger.LogInformation("[Payment] Initiated {Reference} via {Provider}",
            payment.Reference.Reference, provider);

        return new InitiatePaymentResponse
        {
            PaymentId = payment.Id,
            Reference = payment.Reference.Reference,
            Status = payment.Status.ToString(),
            Amount = payment.Amount.Amount,
            CurrencyCode = payment.Amount.CurrencyCode,
            Provider = payment.Provider.ToString(),
            CreatedAt = payment.CreatedAt
        };
    }
}