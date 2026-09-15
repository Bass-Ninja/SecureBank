using Mediator;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.Application.Commands.Transfers;

public sealed class TransferMoneyHandler(
    ISecureBankDbContext dbContext,
    IUserContext userContext)
    : IRequestHandler<TransferMoneyCommand, Guid>
{
    public async ValueTask<Guid> Handle(
        TransferMoneyCommand request,
        CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;

        var sourceAccount = await dbContext.Accounts
            .FirstOrDefaultAsync(
                x => x.Id == request.SourceAccountId
                    && x.UserId == userId,
                cancellationToken);

        if (sourceAccount is null)
            throw new InvalidOperationException(
                "Source account was not found.");
                
        var destinationAccount = await dbContext.Accounts
            .FirstOrDefaultAsync(
                x => x.Id == request.DestinationAccountId,
                cancellationToken);

        if (destinationAccount is null)
            throw new InvalidOperationException(
                "Destination account was not found.");

        var amount = Money.Create(
            request.Amount,
            request.Currency);

        sourceAccount.Debit(amount);
        destinationAccount.Credit(amount);

        var transfer = Transfer.Create(
            sourceAccount.Id,
            destinationAccount.Id,
            amount,
            request.IdempotencyKey);

        transfer.Complete();

        dbContext.Transfers.Add(transfer);

        await dbContext.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }
}