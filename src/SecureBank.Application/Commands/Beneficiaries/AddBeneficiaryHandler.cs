using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Exceptions;
using SecureBank.Domain.Entities;

namespace SecureBank.Application.Commands.Beneficiaries;

public sealed class AddBeneficiaryHandler(
    ISecureBankDbContext dbContext,
    IUserContext userContext)
    : IRequestHandler<AddBeneficiaryCommand, Guid>
{
    public async ValueTask<Guid> Handle(
        AddBeneficiaryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(x => x.AccountNumber == request.AccountNumber, cancellationToken) ?? throw new NotFoundException("The account was not found.");

        if (account.UserId == userId)
        {
            throw new InvalidOperationException(
                "You cannot add your own account as a beneficiary.");
        }

        var existing = await dbContext.Beneficiaries
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.AccountId == account.Id,
                cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var beneficiary = Beneficiary.Create(
            userId,
            account.Id,
            request.Nickname);

        dbContext.Beneficiaries.Add(beneficiary);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            return beneficiary.Id;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation
                  })
        {
            var concurrentBeneficiary = await dbContext.Beneficiaries
                .FirstOrDefaultAsync(
                    x => x.UserId == userId && x.AccountId == account.Id,
                    cancellationToken);

            if (concurrentBeneficiary is not null)
            {
                return concurrentBeneficiary.Id;
            }

            throw;
        }
    }
}
