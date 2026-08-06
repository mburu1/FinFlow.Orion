namespace FinFlow.Orion.Contracts.Common;

public class PagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;

    public int Skip => (Page - 1) * PageSize;
}