using Mediator;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Queries.Accounts.Results;

namespace SecureBank.Application.Queries.Accounts;

public sealed record GetAccountsByUserIdQuery(Guid UserId)
    : IRequireRole, IRequest<Result<IReadOnlyCollection<AccountResult>>>
{
    public IReadOnlyCollection<string> AllowedRoles => BankRoles.Staff;
}
