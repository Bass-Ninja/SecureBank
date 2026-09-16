using Microsoft.AspNetCore.Authorization;

namespace SecureBank.Infrastructure.Authentication;

public sealed class RoleRequirement(params string[] allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> AllowedRoles { get; } = allowedRoles;
}
