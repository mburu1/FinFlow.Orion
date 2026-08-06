using FinFlow.Orion.Application.Common.Interfaces;

namespace FinFlow.Orion.Workers.Services;

/// <summary>
/// Worker-scoped implementation of IDateTimeService.
/// Keeps the Workers project self-contained for testing.
/// </summary>
public sealed class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}