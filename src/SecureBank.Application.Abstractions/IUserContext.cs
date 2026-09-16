namespace SecureBank.Application.Abstractions;

public interface IUserContext
{
    Guid UserId { get; }

    bool IsInRole(string role);
}