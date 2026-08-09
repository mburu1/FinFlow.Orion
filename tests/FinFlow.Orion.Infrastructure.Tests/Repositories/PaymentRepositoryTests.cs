using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.ValueObjects;
using FinFlow.Orion.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Infrastructure.Tests.Repositories;

[Collection(InfrastructureTestCollection.Name)]
public sealed class PaymentRepositoryTests
{
    private readonly InfrastructureTestFixture _fixture;

    public PaymentRepositoryTests(InfrastructureTestFixture fixture) => _fixture = fixture;

    private static Payment CreatePayment(string idempotencyKeySuffix) =>
        Payment.Create(
            new Money(750, "KES"),
            PaymentProvider.Card,
            PaymentChannel.Web,
            new IdempotencyKey($"repo-test-{idempotencyKeySuffix}-{Guid.NewGuid():N}"),
            customerId: "cust-repo-test");

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsThePayment()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new PaymentRepository(dbContext);

        var payment = CreatePayment("by-id");
        await repository.AddAsync(payment);
        await dbContext.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(payment.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Reference.Reference.Should().Be(payment.Reference.Reference);
        reloaded.Amount.Amount.Should().Be(750);
        reloaded.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetByReferenceAsync_ReturnsTheMatchingPayment()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new PaymentRepository(dbContext);

        var payment = CreatePayment("by-reference");
        await repository.AddAsync(payment);
        await dbContext.SaveChangesAsync();

        var reloaded = await repository.GetByReferenceAsync(payment.Reference.Reference);

        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task GetByProviderTransactionIdAsync_FiltersOnTheOwnedTypeColumn()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new PaymentRepository(dbContext);

        var payment = CreatePayment("by-provider-tx-id");
        var providerTxId = $"CARD-{Guid.NewGuid():N}";
        payment.MarkAsAuthorized(new ProviderResponse(providerTxId, "PENDING"));

        await repository.AddAsync(payment);
        await dbContext.SaveChangesAsync();

        var reloaded = await repository.GetByProviderTransactionIdAsync(providerTxId);

        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(payment.Id);
        reloaded.ProviderResponse.Should().NotBeNull();
        reloaded.ProviderResponse!.ProviderTransactionId.Should().Be(providerTxId);
    }

    [Fact]
    public async Task GetByProviderTransactionIdAsync_UnknownId_ReturnsNull()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new PaymentRepository(dbContext);

        var reloaded = await repository.GetByProviderTransactionIdAsync($"does-not-exist-{Guid.NewGuid():N}");

        reloaded.Should().BeNull();
    }
}
