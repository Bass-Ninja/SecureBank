using Mediator;

namespace SecureBank.Application.Commands.Beneficiaries;

public sealed record AddBeneficiaryCommand(
    Guid AccountId,
    string Nickname) : IRequest<Guid>;
