using Mediator;
using Microsoft.Extensions.Logging;
using SecureBank.Domain.Events;

namespace SecureBank.Application.EventHandlers;

public sealed class TransferCompletedEventHandler(
    ILogger<TransferCompletedEventHandler> logger)
    : INotificationHandler<TransferCompletedEvent>
{
    public ValueTask Handle(
        TransferCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Transfer {TransferId} completed: {SourceAccountId} -> {DestinationAccountId}",
            notification.TransferId,
            notification.SourceAccountId,
            notification.DestinationAccountId);

        return ValueTask.CompletedTask;
    }
}
