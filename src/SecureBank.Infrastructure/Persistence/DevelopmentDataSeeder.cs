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
        var user1Id = Guid.Parse("0406f376-1a90-45a8-a117-09a96c051983");
        var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var account1 = await GetOrCreateAccountAsync(
            dbContext,
            user1Id,
            "SI560000000000000001",
            1000m,
            cancellationToken);

        var account2 = await GetOrCreateAccountAsync(
            dbContext,
            user2Id,
            "SI560000000000000002",
            500m,
            cancellationToken);

        var beneficiaryExists = await dbContext.Beneficiaries.AnyAsync(
            beneficiary => beneficiary.UserId == user1Id
                && beneficiary.AccountId == account2.Id,
            cancellationToken);

        if (!beneficiaryExists)
        {
            dbContext.Beneficiaries.Add(
                Beneficiary.Create(
                    user1Id,
                    account2.Id,
                    "Demo recipient"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Account> GetOrCreateAccountAsync(
        SecureBankDbContext dbContext,
        Guid userId,
        string accountNumber,
        decimal balance,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(
            item => item.AccountNumber == accountNumber,
            cancellationToken);

        if (account is not null)
        {
            return account;
        }

        account = Account.Create(
            userId,
            accountNumber,
            Money.Eur(balance));

        dbContext.Accounts.Add(account);

        return account;
    }
}
