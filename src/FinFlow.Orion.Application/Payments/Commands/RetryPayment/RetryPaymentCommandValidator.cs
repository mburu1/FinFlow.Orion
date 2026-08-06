using FluentValidation;

namespace FinFlow.Orion.Application.Payments.Commands.RetryPayment;

public sealed class RetryPaymentCommandValidator
    : AbstractValidator<RetryPaymentCommand>
{
    public RetryPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("Payment ID is required.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MinimumLength(16).WithMessage("Idempotency key must be at least 16 characters.");
    }
}