using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecureBank.Application.Abstractions.Database;
using SecureBank.Domain.Abstractions;
using SecureBank.Domain.Events;

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

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var entitiesWithEvents = ChangeTracker.Entries<Entity>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(x => x.DomainEvents)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        return domainEvents;
    }
}
