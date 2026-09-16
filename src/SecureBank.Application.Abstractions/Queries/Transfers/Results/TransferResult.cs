namespace SecureBank.Application.Abstractions.Queries.Transfers.Results;
public sealed record TransferResult(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);
