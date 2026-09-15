using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Entities;

namespace SecureBank.Application.Abstractions;

public interface ISecureBankDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Transfer> Transfers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}