namespace FinFlow.Orion.Infrastructure.Persistence.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public sealed class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;
    public string AggregateId { get; private set; } = null!;
    public string AggregateType { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; } = 0;
    public DateTime? NextRetryAt { get; private set; }

    private OutboxMessage() { } // EF Core

    public static OutboxMessage Create(
        string type,
        string payload,
        string aggregateId,
        string aggregateType)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void StartProcessing()
    {
        Status = OutboxMessageStatus.Processing;
    }

    public void MarkAsCompleted()
    {
        Status = OutboxMessageStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
        Status = RetryCount >= 3 ? OutboxMessageStatus.Failed : OutboxMessageStatus.Pending;
        NextRetryAt = RetryCount < 3 ? DateTime.UtcNow.AddMinutes(RetryCount * 5) : null;
    }

    public bool IsReadyForRetry =>
        Status == OutboxMessageStatus.Pending &&
        NextRetryAt.HasValue &&
        NextRetryAt.Value <= DateTime.UtcNow;
}