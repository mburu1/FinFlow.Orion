namespace FinFlow.Orion.Domain.Enums;

public enum ReconciliationStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    DiscrepancyFound = 3,
    ManualReview = 4
}