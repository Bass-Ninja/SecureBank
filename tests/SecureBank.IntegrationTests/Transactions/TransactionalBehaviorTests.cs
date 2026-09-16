using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions.Database;
using SecureBank.Application.Behaviors;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;
using SecureBank.Infrastructure.Persistence;
using SecureBank.IntegrationTests.Fixtures;
using Mediator;

namespace SecureBank.IntegrationTests.Transactions;

public sealed class TransactionalBehaviorTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Handle_HandlerThrows_RollsBackChanges()
    {
        var userId = Guid.NewGuid();

        Guid accountId;

        await using (var setupContext = fixture.CreateDbContext())
        {
            var account = Account.Create(
                userId,
                $"SI5600000000000000{Random.Shared.Next(100, 999)}",
                Money.Eur(1000m));

            setupContext.Accounts.Add(account);

            await setupContext.SaveChangesAsync();

            accountId = account.Id;
        }

        await using var context = fixture.CreateDbContext();

        ITransactionalContext transactionalContext = context;

        var behavior = new TransactionalBehavior<TestRequest, Unit>([transactionalContext], new NoOpPublisher());

        var request = new TestRequest();

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await behavior.Handle(
                request,
                async (_, cancellationToken) =>
                {
                    var account = await context.Accounts
                        .SingleAsync(
                            x => x.Id == accountId,
                            cancellationToken);

                    account.Debit(Money.Eur(100m));

                    await context.SaveChangesAsync(cancellationToken);

                    throw new InvalidOperationException(
                        "Simulated failure.");
                },
                CancellationToken.None));

        await using var verificationContext =
            fixture.CreateDbContext();

        var accountAfterFailure =
            await verificationContext.Accounts
                .SingleAsync(x => x.Id == accountId);

        Assert.Equal(
            1000m,
            accountAfterFailure.Balance.Amount);
    }

    private sealed record TestRequest : IRequest<Unit>;

    private sealed class NoOpPublisher : IPublisher
    {
        public ValueTask Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask Publish(
            object notification,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
