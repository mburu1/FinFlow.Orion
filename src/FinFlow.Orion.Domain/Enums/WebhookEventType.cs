namespace FinFlow.Orion.Domain.Enums;

public enum WebhookEventType
{
    PaymentInitiated = 0,
    PaymentCompleted = 1,
    PaymentFailed = 2,
    PaymentReversed = 3,
    RefundRequested = 4
}