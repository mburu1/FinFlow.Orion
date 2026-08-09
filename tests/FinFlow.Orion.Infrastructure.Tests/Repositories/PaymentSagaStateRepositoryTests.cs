using FinFlow.Orion.Application.Sagas;
using FinFlow.Orion.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Infrastructure.Tests.Repositories;

[Collection(InfrastructureTestCollection.Name)]
public sealed class PaymentSagaStateRepositoryTests
{
    private readonly InfrastructureTestFixture _fixture;

    public PaymentSagaStateRepositoryTests(InfrastructureTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AddAsync_ThenReload_RoundTripsCompletedStepsCsvConversion()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new PaymentSagaStateRepository(dbContext);

        var paymentId = Guid.NewGuid();
        var state = new PaymentSagaState
        {
            PaymentId = paymentId,
            CurrentStep = "FallbackTo:Card",
            StartedAt = DateTime.UtcNow
        };
        state.CompletedSteps.Add("PaymentInitiated");
        state.CompletedSteps.Add("FallbackTo:Card");

        await repository.AddAsync(state);

        await using var freshDbContext = _fixture.CreateDbContext();
        var freshRepository = new PaymentSagaStateRepository(freshDbContext);
        var reloaded = await freshRepository.GetActiveByPaymentIdAsync(paymentId);

        reloaded.Should().NotBeNull();
        reloaded!.CompletedSteps.Should().Equal("PaymentInitiated", "FallbackTo:Card");
        reloaded.CurrentStep.Should().Be("FallbackTo:Card");
    }

    [Fact]
    public async Task GetActiveByPaymentIdAsync_ExcludesCompletedSagas()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new PaymentSagaStateRepository(dbContext);

        var paymentId = Guid.NewGuid();
        var state = new PaymentSagaState
        {
            PaymentId = paymentId,
            CurrentStep = "Terminal",
            StartedAt = DateTime.UtcNow,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow
        };

        await repository.AddAsync(state);

        var active = await repository.GetActiveByPaymentIdAsync(paymentId);

        active.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new PaymentSagaStateRepository(dbContext);

        var paymentId = Guid.NewGuid();
        var state = new PaymentSagaState { PaymentId = paymentId, CurrentStep = "PaymentInitiated", StartedAt = DateTime.UtcNow };
        await repository.AddAsync(state);

        state.RetryCount = 2;
        state.LastFailureReason = "provider timeout";
        await repository.UpdateAsync(state);

        await using var freshDbContext = _fixture.CreateDbContext();
        var reloaded = await new PaymentSagaStateRepository(freshDbContext).GetActiveByPaymentIdAsync(paymentId);

        reloaded!.RetryCount.Should().Be(2);
        reloaded.LastFailureReason.Should().Be("provider timeout");
    }
}
