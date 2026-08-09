using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        var act = () => new Money(-1, "KES");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("KE")]
    [InlineData("KESH")]
    public void Constructor_InvalidCurrencyCode_Throws(string currencyCode)
    {
        var act = () => new Money(10, currencyCode);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_UppercasesCurrencyCode()
    {
        var money = new Money(10, "kes");

        money.CurrencyCode.Should().Be("KES");
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var result = new Money(100, "KES").Add(new Money(50, "KES"));

        result.Amount.Should().Be(150);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var act = () => new Money(100, "KES").Add(new Money(50, "USD"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var result = new Money(100, "KES").Subtract(new Money(30, "KES"));

        result.Amount.Should().Be(70);
    }

    [Fact]
    public void Subtract_DifferentCurrency_Throws()
    {
        var act = () => new Money(100, "KES").Subtract(new Money(30, "USD"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = new Money(100, "KES");
        var b = new Money(100, "KES");

        a.Should().Be(b);
    }
}
