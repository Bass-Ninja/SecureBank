namespace SecureBank.Application.Abstractions.Models;

public record PaginationRequest : IPaginationRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}
