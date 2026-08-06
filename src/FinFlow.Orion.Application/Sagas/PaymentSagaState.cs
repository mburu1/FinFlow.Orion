namespace FinFlow.Orion.Application.Sagas;

public sealed class PaymentSagaState
{
    public Guid PaymentId { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? LastFailureReason { get; set; }
    public string? FallbackProvider { get; set; }
    public bool IsCompensating { get; set; }
    public bool IsCompleted { get; set; }
    public List<string> CompletedSteps { get; set; } = [];
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public bool CanRetry => RetryCount < MaxRetries && !IsCompleted;
}