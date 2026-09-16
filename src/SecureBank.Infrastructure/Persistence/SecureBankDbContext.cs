using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Persistence;

public sealed class SecureBankDbContext(DbContextOptions<SecureBankDbContext> options) : TransactionalContext<SecureBankDbContext>(options), ISecureBankDbContext
{ 
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SecureBankDbContext).Assembly);
    }
}
