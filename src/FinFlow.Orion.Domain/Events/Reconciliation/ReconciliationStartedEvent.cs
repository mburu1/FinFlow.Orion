using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Events.Reconciliation;

public sealed class ReconciliationStartedEvent : DomainEvent
{
    public Guid ReportId { get; }
    public PaymentProvider Provider { get; }
    public DateOnly ReconDate { get; }

    public ReconciliationStartedEvent(
        Guid reportId,
        PaymentProvider provider,
        DateOnly reconDate)
    {
        ReportId = reportId;
        Provider = provider;
        ReconDate = reconDate;
    }
}