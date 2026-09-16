using Mediator;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Queries.Accounts.Results;

namespace SecureBank.Application.Queries.Accounts;

public sealed record GetAccountsQuery : IRequest<Result<IReadOnlyCollection<AccountResult>>>;
