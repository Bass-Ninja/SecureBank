namespace SecureBank.Api.Models.Transfers;

public sealed record TransferResponse(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);
