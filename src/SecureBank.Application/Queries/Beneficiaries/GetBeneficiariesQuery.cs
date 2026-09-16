using Mediator;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Abstractions.Queries.Beneficiaries.Results;

namespace SecureBank.Application.Queries.Beneficiaries;

public sealed record GetBeneficiariesQuery : IRequest<Result<IReadOnlyCollection<BeneficiaryResult>>>;
