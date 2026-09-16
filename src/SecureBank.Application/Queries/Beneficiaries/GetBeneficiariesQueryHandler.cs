using Mapster;
using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Abstractions.Queries.Beneficiaries.Results;

namespace SecureBank.Application.Queries.Beneficiaries;

public sealed class GetBeneficiariesQueryHandler(
    ISecureBankDbContext dbContext,
    IUserContext userContext)
    : IRequestHandler<
        GetBeneficiariesQuery,
        Result<IReadOnlyCollection<BeneficiaryResult>>>
{
    public async ValueTask<Result<IReadOnlyCollection<BeneficiaryResult>>> Handle(
        GetBeneficiariesQuery request,
        CancellationToken cancellationToken)
    {
        var beneficiaries = await dbContext.Beneficiaries
            .AsNoTracking()
            .Where(x => x.UserId == userContext.UserId)
            .OrderBy(x => x.Nickname)
            .ProjectToType<BeneficiaryResult>()
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<BeneficiaryResult>>.Ok(beneficiaries);
    }
}
