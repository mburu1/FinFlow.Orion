using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Reconciliation;

public sealed class ReconciliationItem : Entity
{
    public Guid ReportId { get; private set; }
    public string PaymentReference { get; private set; } = null!;
    public string? ProviderTransactionId { get; private set; }
    public Money InternalAmount { get; private set; } = null!;
    public Money ProviderAmount { get; private set; } = null!;
    public PaymentStatus InternalStatus { get; private set; }
    public string ProviderStatus { get; private set; } = null!;
    public bool IsMatched { get; private set; }
    public DateTime TransactionDate { get; private set; }

    private ReconciliationItem() { } // EF Core

    public static ReconciliationItem Create(
        Guid reportId,
        string paymentReference,
        Money internalAmount,
        Money providerAmount,
        PaymentStatus internalStatus,
        string providerStatus,
        DateTime transactionDate,
        string? providerTransactionId = null)
    {
        var item = new ReconciliationItem
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            PaymentReference = paymentReference,
            InternalAmount = internalAmount,
            ProviderAmount = providerAmount,
            InternalStatus = internalStatus,
            ProviderStatus = providerStatus,
            TransactionDate = transactionDate,
            ProviderTransactionId = providerTransactionId
        };

        item.IsMatched = item.InternalAmount == item.ProviderAmount
                      && item.InternalStatus == PaymentStatus.Captured
                      && providerStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);

        return item;
    }
}