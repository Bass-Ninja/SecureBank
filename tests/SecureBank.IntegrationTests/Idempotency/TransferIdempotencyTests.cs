using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;
using SecureBank.IntegrationTests.Fixtures;

namespace SecureBank.IntegrationTests.Idempotency;

public sealed class TransferIdempotencyTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Transfer_SameIdempotencyKeyUsedTwice_ReturnsSameTransfer()
    {
        var userId = Guid.NewGuid();

        Guid sourceAccountId;
        Guid destinationAccountId;

        await using (var setupContext = fixture.CreateDbContext())
        {
            var sourceAccount = Account.Create(
                userId,
                $"SI5600000000000000{Random.Shared.Next(100, 999)}",
                Money.Eur(1000m));

            var destinationAccount = Account.Create(
                Guid.NewGuid(),
                $"SI5600000000000000{Random.Shared.Next(100, 999)}",
                Money.Eur(0m));

            setupContext.Accounts.AddRange(
                sourceAccount,
                destinationAccount);

            await setupContext.SaveChangesAsync();

            sourceAccountId = sourceAccount.Id;
            destinationAccountId = destinationAccount.Id;
        }

        await using var context = fixture.CreateDbContext();

        var idempotentSourceAccount = await context.Accounts
            .SingleAsync(x => x.Id == sourceAccountId);

        var idempotentDestinationAccount = await context.Accounts
            .SingleAsync(x => x.Id == destinationAccountId);

        var amount = Money.Eur(100m);
        const string idempotencyKey = "transfer-001";

        idempotentSourceAccount.Debit(amount);
        idempotentDestinationAccount.Credit(amount);

        var transfer = Transfer.Create(
            userId,
            idempotentSourceAccount.Id,
            idempotentDestinationAccount.Id,
            amount,
            idempotencyKey);

        transfer.Complete();

        context.Transfers.Add(transfer);

        await context.SaveChangesAsync();

        var firstTransferId = transfer.Id;

        var existingTransfer = await context.Transfers
            .SingleAsync(
                x => x.UserId == userId
                    && x.IdempotencyKey == idempotencyKey);

        var sourceBalanceAfterFirstTransfer =
            idempotentSourceAccount.Balance.Amount;

        Assert.Equal(firstTransferId, existingTransfer.Id);
        Assert.Equal(900m, sourceBalanceAfterFirstTransfer);
    }
}
