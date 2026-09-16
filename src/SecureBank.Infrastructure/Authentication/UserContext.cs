using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SecureBank.Application.Abstractions;

namespace SecureBank.Infrastructure.Authentication;

public sealed class UserContext(
    IHttpContextAccessor httpContextAccessor)
    : IUserContext
{
    public Guid UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new InvalidOperationException(
                    "Authenticated user ID is missing or invalid.");
            }

            return parsedUserId;
        }
    }

    public bool IsInRole(string role)
    {
        return httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
    }
}