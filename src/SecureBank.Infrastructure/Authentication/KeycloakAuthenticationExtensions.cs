using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SecureBank.Application.Abstractions;

namespace SecureBank.Infrastructure.Authentication;

public static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException(
                "Keycloak authority is not configured.");

        var issuer = configuration["Keycloak:Issuer"]
            ?? throw new InvalidOperationException(
                "Keycloak issuer is not configured.");

        var audience = configuration["Keycloak:Audience"]
            ?? throw new InvalidOperationException(
                "Keycloak audience is not configured.");

        var requireHttpsMetadata = configuration.GetValue(
            "Keycloak:RequireHttpsMetadata",
            true);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        MapRealmRolesToRoleClaims(context.Principal);

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.BankStaff,
                policy => policy.Requirements.Add(new RoleRequirement(BankRoles.Staff)));

            options.AddPolicy(
                AccountAccessAuthorizationHandler.PolicyName,
                policy => policy.Requirements.Add(new AccountAccessRequirement()));
        });

        services.AddScoped<IAuthorizationHandler, AccountAccessAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

        return services;
    }

    private static void MapRealmRolesToRoleClaims(ClaimsPrincipal? principal)
    {
        var identity = principal?.Identity as ClaimsIdentity;

        var realmAccess = principal?.FindFirst("realm_access")?.Value;

        if (identity is null || realmAccess is null)
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);

        if (!document.RootElement.TryGetProperty("roles", out var roles))
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var roleName = role.GetString();

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }
    }
}
