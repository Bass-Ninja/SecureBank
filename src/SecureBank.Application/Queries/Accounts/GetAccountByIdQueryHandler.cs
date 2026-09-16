using Mapster;
using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Exceptions;
using SecureBank.Application.Queries.Accounts.Results;

namespace SecureBank.Application.Queries.Accounts;

public sealed class GetAccountByIdQueryHandler(
    ISecureBankDbContext dbContext)
    : IRequestHandler<GetAccountByIdQuery, AccountResult>
{
    public async ValueTask<AccountResult> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.Id == request.AccountId)
            .ProjectToType<AccountResult>()
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            throw new NotFoundException("The account was not found.");
        }

        return account;
    }
}
