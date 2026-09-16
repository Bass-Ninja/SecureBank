using Mediator;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Queries.Transfers.Results;

namespace SecureBank.Application.Queries.Transfers;

public sealed record GetTransfersQuery(int Page, int PageSize, string? SortBy, string? SortDirection) : IRequest<Result<PagedResult<TransferResult>>>;
