using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Api.Extensions;
using SecureBank.Api.Models.Transfers;
using SecureBank.Application.Commands.Transfers;
using SecureBank.Application.Queries.Transfers;

namespace SecureBank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transfers")]
public sealed class TransfersController(IMediator sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetTransfersRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTransfersQuery(request.Page, request.PageSize, request.SortBy, request.SortDirection, request.AccountId, request.Status), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new TransferMoneyCommand(
            request.SourceAccountId,
            request.DestinationAccountId,
            request.Amount,
            request.Currency,
            idempotencyKey);

        var transferId = await sender.Send(command, cancellationToken);

        return Ok(new
        {
            transferId
        });
    }
}
