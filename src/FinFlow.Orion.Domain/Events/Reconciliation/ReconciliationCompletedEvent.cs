using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Events.Reconciliation;

public sealed class ReconciliationCompletedEvent : DomainEvent
{
    public Guid ReportId { get; }
    public string ReportReference { get; }
    public PaymentProvider Provider { get; }
    public int MatchedCount { get; }
    public int UnmatchedCount { get; }

    public ReconciliationCompletedEvent(
        Guid reportId,
        string reportReference,
        PaymentProvider provider,
        int matchedCount,
        int unmatchedCount)
    {
        ReportId = reportId;
        ReportReference = reportReference;
        Provider = provider;
        MatchedCount = matchedCount;
        UnmatchedCount = unmatchedCount;
    }
}