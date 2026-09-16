using Mediator;
using SecureBank.Application.Abstractions;
using SecureBank.Application.Exceptions;

namespace SecureBank.Application.Behaviors;

internal sealed class AuthorizationBehavior<TRequest, TResponse>(
    IUserContext userContext)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (message is IRequireRole requireRole
            && !requireRole.AllowedRoles.Any(userContext.IsInRole))
        {
            throw new ForbiddenException(
                $"Requires one of the following roles: {string.Join(", ", requireRole.AllowedRoles)}.");
        }

        return next(message, cancellationToken);
    }
}
