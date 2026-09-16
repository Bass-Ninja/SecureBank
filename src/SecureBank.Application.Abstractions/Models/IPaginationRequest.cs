namespace SecureBank.Application.Abstractions.Models;

public interface IPaginationRequest
{
    int Page { get; }
    int PageSize { get; }
    string? SortBy { get; }
    string? SortDirection { get; }
}
