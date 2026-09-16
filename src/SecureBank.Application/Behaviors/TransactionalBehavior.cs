using Mediator;
using SecureBank.Application.Abstractions.Database;

namespace SecureBank.Application.Behaviors;

internal sealed class TransactionalBehavior<TRequest, TResponse>(IEnumerable<ITransactionalContext> transactionalContexts)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        foreach (var context in transactionalContexts)
        {
            await context.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            TResponse response = await next(message, cancellationToken);

            foreach (var context in transactionalContexts)
            {
                await context.CommitTransactionAsync(cancellationToken);
            }

            return response;
        }
        catch
        {
            foreach (var context in transactionalContexts)
            {
                await context.RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
    }
}
