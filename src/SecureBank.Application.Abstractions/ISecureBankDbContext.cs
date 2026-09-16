using Microsoft.EntityFrameworkCore;
using SecureBank.Domain.Abstractions;
using SecureBank.Domain.Entities;

namespace SecureBank.Application.Abstractions;

public interface ISecureBankDbContext : IUnitOfWork
{
    DbSet<Account> Accounts { get; }
    DbSet<Transfer> Transfers { get; }
    DbSet<Beneficiary> Beneficiaries { get; }
}
