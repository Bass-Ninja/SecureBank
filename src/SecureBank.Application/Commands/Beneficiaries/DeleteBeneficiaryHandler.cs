using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Models;
using SecureBank.Application.Exceptions;

namespace SecureBank.Application.Commands.Beneficiaries;

public sealed class DeleteBeneficiaryHandler(
    ISecureBankDbContext dbContext,
    IUserContext userContext)
    : IRequestHandler<DeleteBeneficiaryCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteBeneficiaryCommand request,
        CancellationToken cancellationToken)
    {
        var beneficiary = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                x => x.Id == request.BeneficiaryId && x.UserId == userContext.UserId,
                cancellationToken);

        if (beneficiary is null)
        {
            throw new NotFoundException("The beneficiary was not found.");
        }

        dbContext.Beneficiaries.Remove(beneficiary);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
