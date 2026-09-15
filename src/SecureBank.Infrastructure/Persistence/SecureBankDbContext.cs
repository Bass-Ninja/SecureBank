using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Persistence;

public sealed class SecureBankDbContext : DbContext, ISecureBankDbContext
{
    public SecureBankDbContext(
        DbContextOptions<SecureBankDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SecureBankDbContext).Assembly);
    }
}