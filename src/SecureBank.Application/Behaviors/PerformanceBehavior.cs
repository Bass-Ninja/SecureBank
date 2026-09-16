using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace SecureBank.Application.Behaviors;

internal sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private const long SlowRequestThresholdMilliseconds = 500;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next(message, cancellationToken);

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMilliseconds)
        {
            logger.LogWarning(
                "{RequestName} took {ElapsedMilliseconds}ms, exceeding the {ThresholdMilliseconds}ms threshold",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                SlowRequestThresholdMilliseconds);
        }

        return response;
    }
}
