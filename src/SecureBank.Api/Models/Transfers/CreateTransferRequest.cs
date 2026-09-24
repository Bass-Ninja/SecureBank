namespace SecureBank.Api.Models.Transfers;

public sealed record CreateTransferRequest(
    Guid SourceAccountId,
    string DestinationAccountNumber,
    decimal Amount,
    string Currency);

