using Mediator;

namespace SecureBank.Application.Commands.Beneficiaries;

public sealed record AddBeneficiaryCommand(
    string AccountNumber,
    string Nickname) : IRequest<Guid>;
