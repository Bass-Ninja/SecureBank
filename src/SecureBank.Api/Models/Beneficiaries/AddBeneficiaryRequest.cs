namespace SecureBank.Api.Models.Beneficiaries;

public sealed record AddBeneficiaryRequest(
    Guid AccountId,
    string Nickname);
