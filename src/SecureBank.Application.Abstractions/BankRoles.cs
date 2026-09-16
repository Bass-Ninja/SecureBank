namespace SecureBank.Application.Abstractions;

public static class BankRoles
{
    public const string Customer = "customer";
    public const string Support = "support";
    public const string Admin = "admin";

    public static readonly string[] Staff = [Support, Admin];
}

public static class UserContextExtensions
{
    public static bool IsBankStaff(this IUserContext userContext)
    {
        return BankRoles.Staff.Any(userContext.IsInRole);
    }
}
