using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;
using SecureBank.IntegrationTests.Fixtures;

namespace SecureBank.IntegrationTests.Concurrency;

public sealed class TransferConcurrencyTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Transfer_TwoConcurrentUpdatesOnSameAccount_ThrowsConcurrencyException()
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

        await using var context1 = fixture.CreateDbContext();
        await using var context2 = fixture.CreateDbContext();

        var sourceAccount1 = await context1.Accounts
            .SingleAsync(x => x.Id == sourceAccountId);

        var sourceAccount2 = await context2.Accounts
            .SingleAsync(x => x.Id == sourceAccountId);

        var destinationAccount1 = await context1.Accounts
            .SingleAsync(x => x.Id == destinationAccountId);

        var destinationAccount2 = await context2.Accounts
            .SingleAsync(x => x.Id == destinationAccountId);

        sourceAccount1.Debit(Money.Eur(100m));
        destinationAccount1.Credit(Money.Eur(100m));

        sourceAccount2.Debit(Money.Eur(100m));
        destinationAccount2.Credit(Money.Eur(100m));

        await context1.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());
    }
}
