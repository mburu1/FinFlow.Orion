namespace FinFlow.Orion.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    Failed = 3,
    Reversed = 4,
    Refunded = 5
}