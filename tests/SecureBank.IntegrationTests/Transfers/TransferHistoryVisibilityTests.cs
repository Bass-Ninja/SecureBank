using Mapster;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Abstractions.Mapping;
using SecureBank.Application.Queries.Transfers;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;
using SecureBank.IntegrationTests.Fixtures;

namespace SecureBank.IntegrationTests.Transfers;

public sealed class TransferHistoryVisibilityTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task GetTransfers_RecipientSeesIncomingTransfer_ButUnrelatedUserDoesNot()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingConfiguration).Assembly);

        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var unrelatedUserId = Guid.NewGuid();
        Guid transferId;

        await using (var setupContext = fixture.CreateDbContext())
        {
            var source = Account.Create(
                senderId,
                CreateAccountNumber(),
                Money.Eur(500m));
            var destination = Account.Create(
                recipientId,
                CreateAccountNumber(),
                Money.Eur(100m));

            var transfer = Transfer.Create(
                senderId,
                source.Id,
                destination.Id,
                Money.Eur(50m),
                $"history-{Guid.NewGuid():N}");

            transfer.Complete();
            transferId = transfer.Id;

            setupContext.Accounts.AddRange(source, destination);
            setupContext.Transfers.Add(transfer);
            await setupContext.SaveChangesAsync();
        }

        await using var recipientContext = fixture.CreateDbContext();
        var recipientHandler = new GetTransfersHandler(
            recipientContext,
            new TestUserContext(recipientId));

        var recipientResult = await recipientHandler.Handle(
            CreateQuery(),
            CancellationToken.None);

        Assert.Contains(
            recipientResult.Value!.Items,
            transfer => transfer.Id == transferId);

        await using var unrelatedContext = fixture.CreateDbContext();
        var unrelatedHandler = new GetTransfersHandler(
            unrelatedContext,
            new TestUserContext(unrelatedUserId));

        var unrelatedResult = await unrelatedHandler.Handle(
            CreateQuery(),
            CancellationToken.None);

        Assert.DoesNotContain(
            unrelatedResult.Value!.Items,
            transfer => transfer.Id == transferId);
    }

    private static GetTransfersQuery CreateQuery()
    {
        return new GetTransfersQuery(
            1,
            25,
            "createdAt",
            "desc",
            null,
            null);
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
