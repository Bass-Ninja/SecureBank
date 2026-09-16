using Mediator;
using Microsoft.Extensions.Logging;

namespace SecureBank.Application.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next(message, cancellationToken);

            logger.LogInformation("Handled {RequestName}", requestName);

            return response;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "{RequestName} failed: {ExceptionMessage}",
                requestName,
                exception.Message);

            throw;
        }
    }
}
