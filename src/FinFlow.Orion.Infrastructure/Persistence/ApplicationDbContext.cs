using FinFlow.Orion.Application.Sagas;
using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Entities.Reconciliation;
using FinFlow.Orion.Domain.Entities.Webhooks;
using FinFlow.Orion.Domain.Entities.Identity; // Add this
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Infrastructure.Persistence.Configurations;
using FinFlow.Orion.Infrastructure.Idempotency;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrionOutbox = FinFlow.Orion.Infrastructure.Persistence.Outbox.OutboxMessage;

namespace FinFlow.Orion.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    private readonly IMediator _mediator;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    // ── Identity ──────────────────────────────────────────────────────────────
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ── Payments ─────────────────────────────────────────────────────────────
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<PaymentProviderConfig> PaymentProviders => Set<PaymentProviderConfig>();

    // Ledger entities are owned exclusively by LedgerDbContext — see that
    // context's XML doc for the bounded-context rationale. Do not add
    // DbSet<LedgerAccount>/<LedgerEntry>/<JournalEntry> here; they are
    // excluded from this model via Ignore<T>() below.

    // ── Reconciliation ────────────────────────────────────────────────────────
    public DbSet<ReconciliationReport> ReconciliationReports => Set<ReconciliationReport>();
    public DbSet<ReconciliationItem> ReconciliationItems => Set<ReconciliationItem>();
    public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();

    // ── Webhooks ──────────────────────────────────────────────────────────────
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    // ── Outbox ────────────────────────────────────────────────────────────────
    public DbSet<OrionOutbox> OutboxMessages => Set<OrionOutbox>();

    // ── Idempotency ───────────────────────────────────────────────────────────
    public DbSet<IdempotencyRecord> IdempotencyKeys => Set<IdempotencyRecord>();

    // ── Payment Saga ──────────────────────────────────────────────────────────
    public DbSet<PaymentSagaState> PaymentSagaStates => Set<PaymentSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            type => !type.Name.Contains("Ledger") && !type.Name.Contains("Journal"));

        modelBuilder.Ignore<LedgerAccount>();
        modelBuilder.Ignore<LedgerEntry>();
        modelBuilder.Ignore<JournalEntry>();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        await WriteToOutboxAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var aggregates = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);
    }

    private Task WriteToOutboxAsync(CancellationToken cancellationToken)
    {
        var aggregates = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0)
            return Task.CompletedTask;

        var outboxMessages = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents.Select(domainEvent =>
            {
                var (typeName, payload) = Messaging.IntegrationEventMap.Map(domainEvent);
                return OrionOutbox.Create(
                    type: typeName,
                    payload: System.Text.Json.JsonSerializer.Serialize(payload, payload.GetType()),
                    aggregateId: aggregate.Id.ToString(),
                    aggregateType: aggregate.GetType().Name);
            }))
            .ToList();

        OutboxMessages.AddRange(outboxMessages);
        return Task.CompletedTask;
    }
}