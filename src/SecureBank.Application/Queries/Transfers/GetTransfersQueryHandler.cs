using Mapster;
using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Queries.Transfers.Results;

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

        var query = dbContext.Transfers
            .AsNoTracking()
            .Where(x => x.UserId == userContext.UserId)
            .Apply(request);

        var totalItems = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
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
