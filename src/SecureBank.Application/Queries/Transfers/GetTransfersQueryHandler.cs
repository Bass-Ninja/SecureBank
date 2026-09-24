using Mapster;
using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Abstractions.Queries.Transfers.Results;

namespace SecureBank.Application.Queries.Transfers;

public sealed class GetTransfersHandler(
    ISecureBankDbContext dbContext,
    IUserContext userContext)
    : IRequestHandler<
        GetTransfersQuery,
        Result<PagedResult<TransferResult>>>
{
    public async ValueTask<Result<PagedResult<TransferResult>>> Handle(
        GetTransfersQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var ownedAccountIds = dbContext.Accounts
            .Where(account => account.UserId == userContext.UserId)
            .Select(account => account.Id);

        var query = dbContext.Transfers
            .AsNoTracking()
            .Where(transfer =>
                ownedAccountIds.Contains(transfer.SourceAccountId)
                || ownedAccountIds.Contains(transfer.DestinationAccountId))
            .Apply(request);

        var totalItems = await query.CountAsync(
            cancellationToken);

        var items = await query
            .ApplySorting(request)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<TransferResult>()
            .ToListAsync(cancellationToken);

        return Result<PagedResult<TransferResult>>.Ok(
            new PagedResult<TransferResult>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(
                    totalItems / (double)pageSize),
                Items = items
            });
    }
}
