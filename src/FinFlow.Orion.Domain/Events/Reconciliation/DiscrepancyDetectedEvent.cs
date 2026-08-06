using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Events.Reconciliation;

public sealed class DiscrepancyDetectedEvent : DomainEvent
{
    public Guid ReportId { get; }
    public string PaymentReference { get; }
    public Money DifferenceAmount { get; }

    public DiscrepancyDetectedEvent(
        Guid reportId,
        string paymentReference,
        Money differenceAmount)
    {
        ReportId = reportId;
        PaymentReference = paymentReference;
        DifferenceAmount = differenceAmount;
    }
}