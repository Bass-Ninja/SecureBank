using Mediator;
using SecureBank.Application.Abstractions.Models;

namespace SecureBank.Application.Commands.Beneficiaries;

public sealed record DeleteBeneficiaryCommand(Guid BeneficiaryId) : IRequest<Result>;
