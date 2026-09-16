using SecureBank.Domain.Enums;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.Domain.Entities;

public sealed class Account
{
    private Account()
    {
    }

    private Account(
        Guid id,
        Guid userId,
        string accountNumber,
        Money balance)
    {
        Id = id;
        UserId = userId;
        AccountNumber = accountNumber;
        Balance = balance;
        Status = AccountStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AccountNumber { get; private set; } = null!;
    public Money Balance { get; private set; } = null!;
    public AccountStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public uint Version { get; private set; }

    public static Account Create(
        Guid userId,
        string accountNumber,
        Money initialBalance)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new ArgumentException(
                "Account number is required.",
                nameof(accountNumber));
        }

        return new Account(
            Guid.NewGuid(),
            userId,
            accountNumber,
            initialBalance);
    }

    public void Credit(Money amount)
    {
        EnsureActive();
        EnsureSameCurrency(amount);

        Balance = Balance.Add(amount);
    }

    public void Debit(Money amount)
    {
        EnsureActive();
        EnsureSameCurrency(amount);

        Balance = Balance.Subtract(amount);
    }

    public void Freeze()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidOperationException(
                "A closed account cannot be frozen.");
        }

        Status = AccountStatus.Frozen;
    }

    public void Unfreeze()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidOperationException(
                "A closed account cannot be unfrozen.");
        }

        Status = AccountStatus.Active;
    }

    public void Close()
    {
        if (Balance.Amount != 0)
        {
            throw new InvalidOperationException(
                "An account with a non-zero balance cannot be closed.");
        }

        Status = AccountStatus.Closed;
    }

    private void EnsureActive()
    {
        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException(
                "The account is not active.");
        }
    }

    private void EnsureSameCurrency(Money amount)
    {
        if (Balance.Currency != amount.Currency)
        {
            throw new InvalidOperationException(
                $"Currency mismatch: {Balance.Currency} vs {amount.Currency}.");
        }
    }
}
