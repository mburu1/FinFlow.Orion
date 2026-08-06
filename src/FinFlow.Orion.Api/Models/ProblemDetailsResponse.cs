namespace FinFlow.Orion.Api.Models;

public sealed class ProblemDetailsResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string? Instance { get; init; }
    public string? TraceId { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}