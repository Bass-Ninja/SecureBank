using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Exceptions;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.Application.Commands.Transfers;

public sealed class TransferMoneyHandler(
    ISecureBankDbContext dbContext,
    IUserContext userContext)
    : IRequestHandler<TransferMoneyCommand, Guid>
{
    public async ValueTask<Guid> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;

        var existingTransfer = await dbContext.Transfers
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                     && x.IdempotencyKey == request.IdempotencyKey,
                cancellationToken);


        if (existingTransfer is not null)
        {
            return existingTransfer.Id;
        }

        var sourceAccount = await dbContext.Accounts
            .FirstOrDefaultAsync(
                x => x.Id == request.SourceAccountId
                    && x.UserId == userId,
                cancellationToken);

        if (sourceAccount is null)
        {
            throw new ForbiddenException("You are not allowed to transfer from this account.");
        }
                
        var destinationAccount = await dbContext.Accounts
            .FirstOrDefaultAsync(
                x => x.Id == request.DestinationAccountId,
                cancellationToken);

        if (destinationAccount is null)
        {
            throw new InvalidOperationException("Destination account was not found.");
        }

        var amount = Money.Create(
            request.Amount,
            request.Currency);

        try
        {
            sourceAccount.Debit(amount);
            destinationAccount.Credit(amount);

            var transfer = Transfer.Create(
                userId,
                sourceAccount.Id,
                destinationAccount.Id,
                amount,
                request.IdempotencyKey);

            transfer.Complete();

            dbContext.Transfers.Add(transfer);

            await dbContext.SaveChangesAsync(cancellationToken);

            return transfer.Id;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation
                  })
        {
            var concurrentTransfer = await dbContext.Transfers
                .FirstOrDefaultAsync(
                    x => x.UserId == userId
                         && x.IdempotencyKey == request.IdempotencyKey,
                    cancellationToken);

            if (concurrentTransfer is not null)
            {
                return concurrentTransfer.Id;
            }

            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "The account was modified by another transaction. Please retry.");
        }
    }
}
