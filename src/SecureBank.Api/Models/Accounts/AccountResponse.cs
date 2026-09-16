namespace SecureBank.Api.Models.Accounts;

public sealed record AccountResponse(
    Guid Id,
    string AccountNumber,
    decimal Balance,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);
