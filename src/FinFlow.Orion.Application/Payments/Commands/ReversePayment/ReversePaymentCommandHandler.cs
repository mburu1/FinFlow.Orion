using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.Commands.ReversePayment;

public sealed class ReversePaymentCommandHandler
    : IRequestHandler<ReversePaymentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<ReversePaymentCommandHandler> _logger;

    public ReversePaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        IPaymentRepository paymentRepository,
        ILogger<ReversePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ReversePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var cached = await _idempotencyService.GetAsync(request.IdempotencyKey, cancellationToken);
        if (cached is not null)
            throw new Domain.Exceptions.IdempotencyViolationException(request.IdempotencyKey);

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Payments.Payment), request.PaymentId);

        payment.Reverse(request.Reason);

        await _idempotencyService.StoreAsync(
            request.IdempotencyKey,
            payment.Id.ToString(),
            TimeSpan.FromHours(24),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Reversal] Payment {Reference} reversed by {By} — Reason: {Reason}",
            payment.Reference.Reference, request.RequestedBy, request.Reason);

        return true;
    }
}