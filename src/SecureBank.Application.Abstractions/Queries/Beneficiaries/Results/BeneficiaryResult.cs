namespace SecureBank.Application.Abstractions.Queries.Beneficiaries.Results;

public sealed record BeneficiaryResult(
    Guid Id,
    Guid AccountId,
    string Nickname,
    DateTimeOffset CreatedAt);
