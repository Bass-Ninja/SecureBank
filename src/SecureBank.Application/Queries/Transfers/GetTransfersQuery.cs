using Mediator;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Abstractions.Queries.Transfers.Results;
using SecureBank.Domain.Enums;

namespace SecureBank.Application.Queries.Transfers;

public sealed record GetTransfersQuery(int Page, int PageSize, string? SortBy, string? SortDirection, Guid? AccountId, TransferStatus? Status) : IRequest<Result<PagedResult<TransferResult>>>;
