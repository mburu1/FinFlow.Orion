using FinFlow.Orion.Application.Payments.Commands.InitiatePayment;
using FluentValidation.TestHelper;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments.Commands;

public class InitiatePaymentCommandValidatorTests
{
    private readonly InitiatePaymentCommandValidator _validator = new();

    private static InitiatePaymentCommand ValidCardCommand() => new(
        Amount: 100,
        CurrencyCode: "KES",
        Provider: "Card",
        Channel: "Web",
        IdempotencyKey: new string('a', 20),
        CustomerId: "cust-1",
        PhoneNumber: null,
        Description: null);

    [Fact]
    public void Valid_CardCommand_PassesValidation()
        => _validator.TestValidate(ValidCardCommand()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Amount_Zero_HasValidationError()
        => _validator.TestValidate(ValidCardCommand() with { Amount = 0 })
            .ShouldHaveValidationErrorFor(x => x.Amount);

    [Fact]
    public void CurrencyCode_WrongLength_HasValidationError()
        => _validator.TestValidate(ValidCardCommand() with { CurrencyCode = "K" })
            .ShouldHaveValidationErrorFor(x => x.CurrencyCode);

    [Fact]
    public void Provider_Unsupported_HasValidationError()
        => _validator.TestValidate(ValidCardCommand() with { Provider = "Bitcoin" })
            .ShouldHaveValidationErrorFor(x => x.Provider);

    [Fact]
    public void IdempotencyKey_TooShort_HasValidationError()
        => _validator.TestValidate(ValidCardCommand() with { IdempotencyKey = "short" })
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);

    [Fact]
    public void MPesaProvider_WithoutPhoneNumber_HasValidationErrorForPhoneNumber()
        => _validator.TestValidate(ValidCardCommand() with { Provider = "MPesa", PhoneNumber = null })
            .ShouldHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void MPesaProvider_WithPhoneNumber_PassesValidation()
        => _validator.TestValidate(ValidCardCommand() with { Provider = "MPesa", PhoneNumber = "254712345678" })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void BankTransferProvider_WithoutBankFields_HasValidationErrors()
    {
        var result = _validator.TestValidate(ValidCardCommand() with { Provider = "BankTransfer" });

        result.ShouldHaveValidationErrorFor(x => x.BankAccountNumber);
        result.ShouldHaveValidationErrorFor(x => x.BankCode);
        result.ShouldHaveValidationErrorFor(x => x.BankAccountName);
    }

    [Fact]
    public void BankTransferProvider_WithBankFields_PassesValidation()
        => _validator.TestValidate(ValidCardCommand() with
        {
            Provider = "BankTransfer",
            BankAccountNumber = "0011223344",
            BankCode = "011",
            BankAccountName = "Jane Doe"
        }).ShouldNotHaveAnyValidationErrors();
}
