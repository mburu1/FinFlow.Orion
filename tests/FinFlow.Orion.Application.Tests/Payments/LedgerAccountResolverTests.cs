using FinFlow.Orion.Application.Payments;
using FinFlow.Orion.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Application.Tests.Payments;

public class LedgerAccountResolverTests
{
    [Theory]
    [InlineData(PaymentProvider.MPesa, "1001-MPESA-FLOAT", "2001-CUSTOMER-PAYABLE")]
    [InlineData(PaymentProvider.Card, "1002-CARD-SETTLE", "2001-CUSTOMER-PAYABLE")]
    [InlineData(PaymentProvider.BankTransfer, "1003-BANK-SETTLE", "2001-CUSTOMER-PAYABLE")]
    public void ResolveForProvider_ReturnsExpectedAccountPair(
        PaymentProvider provider, string expectedDebit, string expectedCredit)
    {
        var (debit, credit) = LedgerAccountResolver.ResolveForProvider(provider);

        debit.Should().Be(expectedDebit);
        credit.Should().Be(expectedCredit);
    }

    [Theory]
    [InlineData(PaymentProvider.Flutterwave)]
    [InlineData(PaymentProvider.Paystack)]
    public void ResolveForProvider_UnmappedProvider_ThrowsNotSupportedException(PaymentProvider provider)
    {
        var act = () => LedgerAccountResolver.ResolveForProvider(provider);

        act.Should().Throw<NotSupportedException>();
    }
}
