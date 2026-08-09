using FinFlow.Orion.Application.Payments.Commands.RetryPayment;
using FluentValidation.TestHelper;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Commands;

public class RetryPaymentCommandValidatorTests
{
    private readonly RetryPaymentCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_PassesValidation()
        => _validator.TestValidate(new RetryPaymentCommand(Guid.NewGuid(), new string('a', 20)))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void PaymentId_Empty_HasValidationError()
        => _validator.TestValidate(new RetryPaymentCommand(Guid.Empty, new string('a', 20)))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);

    [Fact]
    public void IdempotencyKey_TooShort_HasValidationError()
        => _validator.TestValidate(new RetryPaymentCommand(Guid.NewGuid(), "short"))
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
}
