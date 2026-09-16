using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SecureBank.Application.Abstractions;

namespace SecureBank.Infrastructure.Authentication;

public sealed class AccountAccessAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IUserContext userContext,
    ISecureBankDbContext dbContext)
    : AuthorizationHandler<AccountAccessRequirement>
{
    public const string PolicyName = "AccountAccessPolicy";

    private const string AccountIdKey = "id";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountAccessRequirement requirement)
    {
        if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated != true)
        {
            context.Fail(new AuthorizationFailureReason(this, AuthorizationFailureReasons.NotAuthenticated));
            return;
        }

        if (userContext.IsBankStaff())
        {
            context.Succeed(requirement);
            return;
        }

        var httpRequest = httpContextAccessor.HttpContext.Request;

        if (!TryGetGuid(httpRequest, AccountIdKey, out var accountId))
        {
            context.Fail(new AuthorizationFailureReason(this, AuthorizationFailureReasons.ResourceNotResolved));
            return;
        }

        var isOwner = await dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.Id == accountId && x.UserId == userContext.UserId);

        if (isOwner)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, AuthorizationFailureReasons.NotResourceOwner));
    }

    private static bool TryGetGuid(HttpRequest httpRequest, string key, out Guid value)
    {
        if (httpRequest.RouteValues.TryGetValue(key, out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out value))
        {
            return true;
        }

        if (httpRequest.Query.TryGetValue(key, out var queryValue) &&
            Guid.TryParse(queryValue.ToString(), out value))
        {
            return true;
        }

        value = Guid.Empty;
        return false;
    }
}
