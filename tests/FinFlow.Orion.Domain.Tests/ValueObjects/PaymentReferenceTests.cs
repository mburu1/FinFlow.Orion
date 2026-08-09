using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Domain.Tests.ValueObjects;

public class PaymentReferenceTests
{
    [Fact]
    public void Constructor_TooShort_Throws()
    {
        var act = () => new PaymentReference("SHORT");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_UppercasesReference()
    {
        var reference = new PaymentReference("abcdef123456");

        reference.Reference.Should().Be("ABCDEF123456");
    }

    [Fact]
    public void Generate_ProducesUniqueReferences()
    {
        var first = PaymentReference.Generate();
        var second = PaymentReference.Generate();

        first.Reference.Should().NotBe(second.Reference);
        first.Reference.Should().HaveLength(12);
    }
}
