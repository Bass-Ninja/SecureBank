using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Api.Extensions;
using SecureBank.Application.Queries.Accounts;
using SecureBank.Infrastructure.Authentication;

namespace SecureBank.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BankStaff)]
[Route("api/staff/accounts")]
public sealed class StaffAccountsController(IMediator sender) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountsByUserIdQuery(userId), cancellationToken);

        return result.ToActionResult();
    }
}
