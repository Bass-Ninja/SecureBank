namespace SecureBank.Infrastructure.Authentication;

public static class AuthorizationFailureReasons
{
    public const string NotAuthenticated = "The caller is not authenticated.";
    public const string NoHttpContext = "No HTTP context is available to resolve the resource from.";
    public const string ResourceNotResolved = "The resource could not be resolved from the request.";
    public const string NotResourceOwner = "The caller does not own this resource.";
}
