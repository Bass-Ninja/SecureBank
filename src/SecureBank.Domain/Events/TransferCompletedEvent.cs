namespace SecureBank.Domain.Events;

public sealed record TransferCompletedEvent(
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    DateTimeOffset OccurredAt) : IDomainEvent;