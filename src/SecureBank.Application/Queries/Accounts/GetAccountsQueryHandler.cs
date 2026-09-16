using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Queries.Accounts.Results;

namespace SecureBank.Application.Queries.Accounts;

public sealed class GetAccountsQueryHandler(
    ISecureBankDbContext dbContext,
    IUserContext userContext)
    : IRequestHandler<
        GetAccountsQuery,
        Result<IReadOnlyCollection<AccountResult>>>
{
    public async ValueTask<Result<IReadOnlyCollection<AccountResult>>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.UserId == userContext.UserId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new AccountResult(
                x.Id,
                x.AccountNumber,
                x.Balance.Amount,
                x.Balance.Currency,
                x.Status.ToString(),
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<AccountResult>>.Ok(accounts);
    }
}
