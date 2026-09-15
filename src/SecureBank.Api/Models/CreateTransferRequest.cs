namespace SecureBank.Api.Models.Transfers;

public sealed record CreateTransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency);

