namespace SecureBank.Application.Queries.Accounts.Results;

public sealed record AccountResult(
    Guid Id,
    string AccountNumber,
    decimal Balance,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);
