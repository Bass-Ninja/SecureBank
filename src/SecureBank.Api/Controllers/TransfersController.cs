using Mediator;
using Microsoft.AspNetCore.Mvc;
using SecureBank.Api.Models.Transfers;
using SecureBank.Application.Commands.Transfers;

namespace SecureBank.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public sealed class TransfersController(IMediator mediator) : ControllerBase
{
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

        var transferId = await mediator.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            transferId
        });
    }
}

