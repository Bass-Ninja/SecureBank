using Mediator;

namespace SecureBank.Application.Commands.Transfers;

public sealed record TransferMoneyCommand(
    Guid SourceAccountId,
    string DestinationAccountNumber,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<Guid>;
