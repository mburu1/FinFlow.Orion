using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Fact]
    public void Constructor_EmptyNumber_Throws()
    {
        var act = () => new PhoneNumber("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LocalFormat_NormalizesToInternational()
    {
        var phone = new PhoneNumber("0712345678");

        phone.Number.Should().Be("254712345678");
    }

    [Fact]
    public void Constructor_AlreadyInternational_StaysUnchanged()
    {
        var phone = new PhoneNumber("254712345678");

        phone.Number.Should().Be("254712345678");
    }

    [Fact]
    public void Equality_SameNumberAndCountryCode_AreEqual()
    {
        var a = new PhoneNumber("0712345678");
        var b = new PhoneNumber("254712345678");

        a.Should().Be(b);
    }
}
