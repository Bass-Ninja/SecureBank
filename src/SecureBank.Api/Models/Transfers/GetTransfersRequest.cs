using SecureBank.Application.Abstractions.Models;

namespace SecureBank.Api.Models.Transfers;

public sealed record GetTransfersRequest : PaginationRequest
{
    public string? Status { get; init; }
    public Guid? AccountId { get; init; }
}
