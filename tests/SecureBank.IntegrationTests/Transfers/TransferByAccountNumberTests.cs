using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Commands.Transfers;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;
using SecureBank.IntegrationTests.Fixtures;

namespace SecureBank.IntegrationTests.Transfers;

public sealed class TransferByAccountNumberTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Transfer_WithSpacedLowercaseAccountNumber_MovesMoneyToResolvedAccount()
    {
        var senderId = Guid.NewGuid();
        var destinationAccountNumber = CreateAccountNumber();
        Guid sourceAccountId;
        Guid destinationAccountId;

        await using (var setupContext = fixture.CreateDbContext())
        {
            var source = Account.Create(
                senderId,
                CreateAccountNumber(),
                Money.Eur(500m));
            var destination = Account.Create(
                Guid.NewGuid(),
                destinationAccountNumber,
                Money.Eur(100m));

            sourceAccountId = source.Id;
            destinationAccountId = destination.Id;

            setupContext.Accounts.AddRange(source, destination);
            await setupContext.SaveChangesAsync();
        }

        await using (var commandContext = fixture.CreateDbContext())
        {
            var handler = new TransferMoneyHandler(
                commandContext,
                new TestUserContext(senderId));
            var formattedNumber = string.Join(
                ' ',
                destinationAccountNumber.ToLowerInvariant().Chunk(4).Select(chars => new string(chars)));

            await handler.Handle(
                new TransferMoneyCommand(
                    sourceAccountId,
                    formattedNumber,
                    75m,
                    "EUR",
                    $"account-number-{Guid.NewGuid():N}"),
                CancellationToken.None);
        }

        await using var assertionContext = fixture.CreateDbContext();
        var sourceBalance = await assertionContext.Accounts
            .Where(account => account.Id == sourceAccountId)
            .Select(account => account.Balance.Amount)
            .SingleAsync();
        var destinationBalance = await assertionContext.Accounts
            .Where(account => account.Id == destinationAccountId)
            .Select(account => account.Balance.Amount)
            .SingleAsync();

        Assert.Equal(425m, sourceBalance);
        Assert.Equal(175m, destinationBalance);
    }

    private static string CreateAccountNumber()
    {
        return $"SI56{Random.Shared.NextInt64(0, 9999999999999999):D16}";
    }

    private sealed class TestUserContext(Guid userId) : IUserContext
    {
        public Guid UserId => userId;

        public bool IsInRole(string role) => false;
    }
}
