namespace FinFlow.Orion.Domain.Exceptions;

public class IdempotencyViolationException : DomainException
{
    public IdempotencyViolationException(string idempotencyKey)
        : base($"Duplicate request detected. Idempotency key: {idempotencyKey}") { }
}