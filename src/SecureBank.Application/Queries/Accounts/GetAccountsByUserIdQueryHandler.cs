using Mapster;
using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Queries.Accounts.Results;

namespace SecureBank.Application.Queries.Accounts;

public sealed class GetAccountsByUserIdQueryHandler(
    ISecureBankDbContext dbContext)
    : IRequestHandler<
        GetAccountsByUserIdQuery,
        Result<IReadOnlyCollection<AccountResult>>>
{
    public async ValueTask<Result<IReadOnlyCollection<AccountResult>>> Handle(
        GetAccountsByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.UserId == request.UserId)
            .OrderBy(x => x.CreatedAt)
            .ProjectToType<AccountResult>()
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<AccountResult>>.Ok(accounts);
    }
}
