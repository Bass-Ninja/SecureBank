namespace SecureBank.Api.Models.Beneficiaries;

public sealed record AddBeneficiaryRequest(
    string AccountNumber,
    string Nickname);
