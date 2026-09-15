using SecureBank.Domain.ValueObjects;
using SecureBank.Domain.Events;

namespace SecureBank.Domain.Entities;

public sealed class Transfer
{
    private Transfer()
    {
    }

    private Transfer(
        Guid id,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        string idempotencyKey)
    {
        Id = id;
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        IdempotencyKey = idempotencyKey;
        Status = Enums.TransferStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SourceAccountId { get; private set; }

    public Guid DestinationAccountId { get; private set; }

    public Money Amount { get; private set; } = null!;

    public string IdempotencyKey { get; private set; } = null!;

    public Enums.TransferStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Transfer Create(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        string idempotencyKey)
    {
        if (sourceAccountId == Guid.Empty)
            throw new ArgumentException(
                "Source account is required.",
                nameof(sourceAccountId));

        if (destinationAccountId == Guid.Empty)
            throw new ArgumentException(
                "Destination account is required.",
                nameof(destinationAccountId));

        if (sourceAccountId == destinationAccountId)
            throw new InvalidOperationException(
                "Source and destination accounts must be different.");

        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Transfer amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException(
                "Idempotency key is required.",
                nameof(idempotencyKey));

        return new Transfer(
            Guid.NewGuid(),
            sourceAccountId,
            destinationAccountId,
            amount,
            idempotencyKey);
    }

    public void Complete()
    {
        if (Status != Enums.TransferStatus.Pending)
            throw new InvalidOperationException(
                "Only pending transfers can be completed.");

        Status = Enums.TransferStatus.Completed;

        _domainEvents.Add(
            new TransferCompletedEvent(
                Id,
                SourceAccountId,
                DestinationAccountId,
                DateTimeOffset.UtcNow));
    }

    public void Fail()
    {
        if (Status != Enums.TransferStatus.Pending)
            throw new InvalidOperationException(
                "Only pending transfers can be failed.");

        Status = Enums.TransferStatus.Failed;
    }
}