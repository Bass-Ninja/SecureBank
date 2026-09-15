using Mediator;

namespace SecureBank.Application.Commands.Transfers;

public sealed record TransferMoneyCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<Guid>;