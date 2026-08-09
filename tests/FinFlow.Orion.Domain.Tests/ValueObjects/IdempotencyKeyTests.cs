using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Domain.Tests.ValueObjects;

public class IdempotencyKeyTests
{
    [Fact]
    public void Constructor_TooShort_Throws()
    {
        var act = () => new IdempotencyKey("short");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ValidValue_Succeeds()
    {
        var key = new IdempotencyKey(new string('a', 16));

        key.Value.Should().HaveLength(16);
    }

    [Fact]
    public void Generate_ProducesDistinctKeysForDistinctInputs()
    {
        var first = IdempotencyKey.Generate("req-1", "user-1");
        var second = IdempotencyKey.Generate("req-2", "user-1");

        first.Value.Should().NotBe(second.Value);
    }
}
