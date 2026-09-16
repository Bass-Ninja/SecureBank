using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;
using SecureBank.IntegrationTests.Fixtures;

namespace SecureBank.IntegrationTests.Concurrency;

public sealed class AccountConcurrencyTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task SaveChanges_SameAccountModifiedByAnotherContext_ThrowsConcurrencyException()
    {
        var userId = Guid.NewGuid();

        await using (var setupContext = fixture.CreateDbContext())
        {
            var account = Account.Create(
                userId,
                "SI560000000000000099",
                Money.Eur(1000m));

            setupContext.Accounts.Add(account);

            await setupContext.SaveChangesAsync();
        }

        await using var context1 = fixture.CreateDbContext();
        await using var context2 = fixture.CreateDbContext();

        var account1 = await context1.Accounts
            .SingleAsync();

        var account2 = await context2.Accounts
            .SingleAsync();

        account1.Debit(Money.Eur(100m));

        await context1.SaveChangesAsync();

        account2.Debit(Money.Eur(100m));

        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync());

        Assert.NotNull(exception);
    }
}
