using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Api.Extensions;
using SecureBank.Api.Models.Accounts;
using SecureBank.Application.Queries.Accounts;
using SecureBank.Application.Queries.Accounts.Results;

namespace SecureBank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountsController(IMediator sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountsQuery(), cancellationToken);

        return result.ToActionResult<IReadOnlyCollection<AccountResult>, IReadOnlyCollection<AccountResponse>>();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);

        return result.ToActionResult<AccountResult, AccountResponse>();
    }
}
