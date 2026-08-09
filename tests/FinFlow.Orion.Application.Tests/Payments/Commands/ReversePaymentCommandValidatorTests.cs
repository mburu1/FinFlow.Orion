using FinFlow.Orion.Application.Payments.Commands.ReversePayment;
using FluentValidation.TestHelper;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Commands;

public class ReversePaymentCommandValidatorTests
{
    private readonly ReversePaymentCommandValidator _validator = new();

    private static ReversePaymentCommand ValidCommand() =>
        new(Guid.NewGuid(), "customer requested refund", "admin", new string('a', 20));

    [Fact]
    public void Valid_Command_PassesValidation()
        => _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void PaymentId_Empty_HasValidationError()
        => _validator.TestValidate(ValidCommand() with { PaymentId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.PaymentId);

    [Fact]
    public void Reason_Empty_HasValidationError()
        => _validator.TestValidate(ValidCommand() with { Reason = "" })
            .ShouldHaveValidationErrorFor(x => x.Reason);

    [Fact]
    public void Reason_TooLong_HasValidationError()
        => _validator.TestValidate(ValidCommand() with { Reason = new string('x', 501) })
            .ShouldHaveValidationErrorFor(x => x.Reason);

    [Fact]
    public void RequestedBy_Empty_HasValidationError()
        => _validator.TestValidate(ValidCommand() with { RequestedBy = "" })
            .ShouldHaveValidationErrorFor(x => x.RequestedBy);

    [Fact]
    public void IdempotencyKey_TooShort_HasValidationError()
        => _validator.TestValidate(ValidCommand() with { IdempotencyKey = "short" })
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
}
