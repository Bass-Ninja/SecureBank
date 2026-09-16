using SecureBank.Application.Abstractions.Models;
using SecureBank.Domain.Enums;

namespace SecureBank.Api.Models.Transfers;

public sealed record GetTransfersRequest : PaginationRequest
{
    public TransferStatus? Status { get; init; }
    public Guid? AccountId { get; init; }
}
