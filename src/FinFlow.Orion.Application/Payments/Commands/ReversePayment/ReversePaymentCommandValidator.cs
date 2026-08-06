using FluentValidation;

namespace FinFlow.Orion.Application.Payments.Commands.ReversePayment;

public sealed class ReversePaymentCommandValidator
    : AbstractValidator<ReversePaymentCommand>
{
    public ReversePaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("Payment ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");

        RuleFor(x => x.RequestedBy)
            .NotEmpty().WithMessage("RequestedBy is required.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MinimumLength(16).WithMessage("Idempotency key must be at least 16 characters.");
    }
}