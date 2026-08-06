using FluentValidation;

namespace FinFlow.Orion.Application.Payments.Commands.InitiatePayment;

public sealed class InitiatePaymentCommandValidator
    : AbstractValidator<InitiatePaymentCommand>
{
    private static readonly string[] SupportedProviders = ["MPesa", "Card", "BankTransfer", "Flutterwave", "Paystack"];
    private static readonly string[] SupportedChannels = ["Mobile", "Web", "Api", "POS"];

    public InitiatePaymentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .Length(3).WithMessage("Currency code must be exactly 3 characters (e.g. KES, USD).");

        RuleFor(x => x.Provider)
            .NotEmpty()
            .Must(p => SupportedProviders.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Provider must be one of: {string.Join(", ", SupportedProviders)}.");

        RuleFor(x => x.Channel)
            .NotEmpty()
            .Must(c => SupportedChannels.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Channel must be one of: {string.Join(", ", SupportedChannels)}.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MinimumLength(16).WithMessage("Idempotency key must be at least 16 characters.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{9,15}$")
            .When(x => x.PhoneNumber is not null)
            .WithMessage("Invalid phone number format.");
    }
}