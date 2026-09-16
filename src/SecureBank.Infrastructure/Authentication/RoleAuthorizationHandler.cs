using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using SecureBank.Application.Abstractions;

namespace SecureBank.Infrastructure.Authentication;

public sealed class RoleAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IUserContext userContext)
    : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated != true)
        {
            context.Fail(new AuthorizationFailureReason(this, AuthorizationFailureReasons.NotAuthenticated));
            return Task.CompletedTask;
        }

        if (requirement.AllowedRoles.Any(userContext.IsInRole))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail(new AuthorizationFailureReason(this, AuthorizationFailureReasons.NotInRequiredRole));

        return Task.CompletedTask;
    }
}
