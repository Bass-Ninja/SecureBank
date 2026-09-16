using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecureBank.Application.Abstractions.Database;

namespace SecureBank.Infrastructure.Persistence;

public class TransactionalContext<TContext>(
    DbContextOptions<TContext> options)
    : DbContext(options),
        ITransactionalContext
    where TContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        _currentTransaction =
            await Database.BeginTransactionAsync(
                cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(
            cancellationToken);

        await _currentTransaction.DisposeAsync();

        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync(
            cancellationToken);

        await _currentTransaction.DisposeAsync();

        _currentTransaction = null;
    }
}
