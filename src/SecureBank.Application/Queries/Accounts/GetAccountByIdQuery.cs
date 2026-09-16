using Mediator;
using SecureBank.Application.Queries.Accounts.Results;

namespace SecureBank.Application.Queries.Accounts;

public sealed record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountResult>;
