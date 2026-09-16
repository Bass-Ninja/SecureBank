namespace SecureBank.Application.Abstractions;

public interface IRequireRole
{
    IReadOnlyCollection<string> AllowedRoles { get; }
}
