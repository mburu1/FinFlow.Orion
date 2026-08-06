using FinFlow.Orion.Domain.Abstractions;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Events.Reconciliation;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Reconciliation;

public sealed class ReconciliationReport : AggregateRoot, IAuditableEntity
{
    public string ReportReference { get; private set; } = null!;
    public PaymentProvider Provider { get; private set; }
    public DateOnly ReconDate { get; private set; }
    public ReconciliationStatus Status { get; private set; }
    public int TotalTransactions { get; private set; }
    public int MatchedCount { get; private set; }
    public int UnmatchedCount { get; private set; }
    public Money TotalMatchedAmount { get; private set; } = null!;
    public Money TotalDiscrepancyAmount { get; private set; } = null!;
    public DateTime? CompletedAt { get; private set; }

    private readonly List<ReconciliationItem> _items = [];
    public IReadOnlyCollection<ReconciliationItem> Items => _items.AsReadOnly();

    private readonly List<Discrepancy> _discrepancies = [];
    public IReadOnlyCollection<Discrepancy> Discrepancies => _discrepancies.AsReadOnly();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    private ReconciliationReport() { } // EF Core

    public static ReconciliationReport Create(
        PaymentProvider provider,
        DateOnly reconDate,
        string currencyCode = "KES")
    {
        var report = new ReconciliationReport
        {
            Id = Guid.NewGuid(),
            ReportReference = $"RECON-{provider}-{reconDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            Provider = provider,
            ReconDate = reconDate,
            Status = ReconciliationStatus.Pending,
            TotalMatchedAmount = Money.Zero(currencyCode),
            TotalDiscrepancyAmount = Money.Zero(currencyCode),
            CreatedAt = DateTime.UtcNow
        };

        report.AddDomainEvent(new ReconciliationStartedEvent(report.Id, provider, reconDate));
        return report;
    }

    public void AddItem(ReconciliationItem item)
    {
        _items.Add(item);
        TotalTransactions++;
    }

    public void AddDiscrepancy(Discrepancy discrepancy)
    {
        _discrepancies.Add(discrepancy);
        UnmatchedCount++;
        TotalDiscrepancyAmount = TotalDiscrepancyAmount.Add(discrepancy.DifferenceAmount);
        Status = ReconciliationStatus.DiscrepancyFound;

        AddDomainEvent(new DiscrepancyDetectedEvent(
            Id,
            discrepancy.PaymentReference,
            discrepancy.DifferenceAmount));
    }

    public void Complete()
    {
        MatchedCount = TotalTransactions - UnmatchedCount;
        Status = _discrepancies.Count > 0
            ? ReconciliationStatus.DiscrepancyFound
            : ReconciliationStatus.Completed;

        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReconciliationCompletedEvent(
            Id,
            ReportReference,
            Provider,
            MatchedCount,
            UnmatchedCount));
    }

    public void FlagForManualReview()
    {
        Status = ReconciliationStatus.ManualReview;
        UpdatedAt = DateTime.UtcNow;
    }
}