using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Entities;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(
        SecureBankDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var user1Id = Guid.Parse("0406f376-1a90-45a8-a117-09a96c051983");
        var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var account1 = Account.Create(
            user1Id,
            "SI560000000000000001",
            Money.Eur(1000m));

        var account2 = Account.Create(
            user2Id,
            "SI560000000000000002",
            Money.Eur(500m));

        dbContext.Accounts.AddRange(
            account1,
            account2);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
