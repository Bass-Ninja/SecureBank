namespace SecureBank.Domain.Entities;

public sealed class Beneficiary
{
    private Beneficiary()
    {
    }

    private Beneficiary(
        Guid id,
        Guid userId,
        Guid accountId,
        string nickname)
    {
        Id = id;
        UserId = userId;
        AccountId = accountId;
        Nickname = nickname;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public string Nickname { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Beneficiary Create(
        Guid userId,
        Guid accountId,
        string nickname)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account ID is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException("Nickname is required.", nameof(nickname));
        }

        return new Beneficiary(
            Guid.NewGuid(),
            userId,
            accountId,
            nickname);
    }
}
