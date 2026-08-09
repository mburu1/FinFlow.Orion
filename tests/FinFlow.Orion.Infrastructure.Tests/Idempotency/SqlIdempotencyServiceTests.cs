using FinFlow.Orion.Infrastructure.Idempotency;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinFlow.Orion.Infrastructure.Tests.Idempotency;

[Collection(InfrastructureTestCollection.Name)]
public sealed class SqlIdempotencyServiceTests
{
    private readonly InfrastructureTestFixture _fixture;

    public SqlIdempotencyServiceTests(InfrastructureTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task StoreAsync_ThenGetAsync_ReturnsTheStoredResponse()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new SqlIdempotencyService(dbContext, NullLogger<SqlIdempotencyService>.Instance);

        var key = $"idem-{Guid.NewGuid():N}";
        await service.StoreAsync(key, "cached-response-body");

        var result = await service.GetAsync(key);

        result.Should().Be("cached-response-body");
    }

    [Fact]
    public async Task StoreAsync_DuplicateKey_DoesNotOverwriteTheOriginal()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new SqlIdempotencyService(dbContext, NullLogger<SqlIdempotencyService>.Instance);

        var key = $"idem-dup-{Guid.NewGuid():N}";
        await service.StoreAsync(key, "first-response");
        await service.StoreAsync(key, "second-response");

        var result = await service.GetAsync(key);

        result.Should().Be("first-response");
    }

    [Fact]
    public async Task GetAsync_ExpiredKey_ReturnsNull()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new SqlIdempotencyService(dbContext, NullLogger<SqlIdempotencyService>.Instance);

        var key = $"idem-expired-{Guid.NewGuid():N}";
        await service.StoreAsync(key, "response", TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);

        var result = await service.GetAsync(key);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_UnknownKey_ReturnsNull()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new SqlIdempotencyService(dbContext, NullLogger<SqlIdempotencyService>.Instance);

        var result = await service.GetAsync($"never-stored-{Guid.NewGuid():N}");

        result.Should().BeNull();
    }
}
