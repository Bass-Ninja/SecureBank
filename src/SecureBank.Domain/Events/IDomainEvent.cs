namespace SecureBank.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
