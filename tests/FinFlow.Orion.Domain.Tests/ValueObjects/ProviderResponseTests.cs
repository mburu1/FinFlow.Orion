using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Domain.Tests.ValueObjects;

public class ProviderResponseTests
{
    [Fact]
    public void Constructor_EmptyTransactionId_Throws()
    {
        var act = () => new ProviderResponse("", "SUCCESS");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("SUCCESS", true)]
    [InlineData("success", true)]
    [InlineData("FAILED", false)]
    [InlineData("PENDING", false)]
    public void IsSuccessful_ReflectsStatusCaseInsensitively(string status, bool expected)
    {
        var response = new ProviderResponse("TX-1", status);

        response.IsSuccessful.Should().Be(expected);
    }
}
