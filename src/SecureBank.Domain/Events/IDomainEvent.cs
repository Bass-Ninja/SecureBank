using Mediator;

namespace SecureBank.Domain.Events;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}
