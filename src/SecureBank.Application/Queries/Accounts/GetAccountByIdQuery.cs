using Mediator;
using SecureBank.Application.Abstractions.Queries.Accounts.Results;

namespace SecureBank.Application.Queries.Accounts;

public sealed record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountResult>;
