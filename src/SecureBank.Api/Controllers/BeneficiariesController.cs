using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Api.Extensions;
using SecureBank.Api.Models.Beneficiaries;
using SecureBank.Application.Commands.Beneficiaries;
using SecureBank.Application.Queries.Beneficiaries;

namespace SecureBank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/beneficiaries")]
public sealed class BeneficiariesController(IMediator sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBeneficiariesQuery(), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Add(
        [FromBody] AddBeneficiaryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBeneficiaryCommand(request.AccountId, request.Nickname);

        var beneficiaryId = await sender.Send(command, cancellationToken);

        return Ok(new
        {
            beneficiaryId
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBeneficiaryCommand(id), cancellationToken);

        return result.ToActionResult();
    }
}
