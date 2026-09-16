namespace SecureBank.Application.Abstractions.Models;

public class PagedResult<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public IReadOnlyCollection<T> Items { get; init; } = [];
}
