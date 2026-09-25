using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SecureBank.Application.Abstractions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;

                var metadataAddress =
                    $"{authority}/.well-known/openid-configuration";

                options.ConfigurationManager =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        metadataAddress,
                        new InternalKeycloakConfigurationRetriever(authority),
                        new HttpDocumentRetriever
                        {
                            RequireHttps = requireHttpsMetadata
                        });

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

    internal sealed class InternalKeycloakConfigurationRetriever(
    string authority) : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            string address,
            IDocumentRetriever retriever,
            CancellationToken cancel)
        {
            var document = await retriever.GetDocumentAsync(address, cancel);

            var configuration =
                OpenIdConnectConfiguration.Create(document);

            var jwksAddress =
                $"{authority}/protocol/openid-connect/certs";

            var jwksDocument =
                await retriever.GetDocumentAsync(jwksAddress, cancel);

            var jwks = new JsonWebKeySet(jwksDocument);

            foreach (var signingKey in jwks.GetSigningKeys())
            {
                configuration.SigningKeys.Add(signingKey);
            }

            return configuration;
        }
    }

    private static void MapRealmRolesToRoleClaims(ClaimsPrincipal? principal)
    {
        var realmAccess = principal?.FindFirst("realm_access")?.Value;

        if (principal?.Identity is not ClaimsIdentity identity || realmAccess is null)
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
