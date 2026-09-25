using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

namespace SecureBank.Infrastructure.Documentation;

public sealed class OAuthSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IConfiguration configuration)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes =
            await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.All(scheme => scheme.Name != "Bearer"))
        {
            return;
        }

        var issuer =
            configuration["Keycloak:Issuer"]
            ?? throw new InvalidOperationException(
                "Keycloak:Issuer is not configured.");

        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes = new Dictionary<
            string,
            IOpenApiSecurityScheme>
        {
            ["OAuth2"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(
                            $"{issuer}/protocol/openid-connect/auth"),

                        TokenUrl = new Uri(
                            $"{issuer}/protocol/openid-connect/token"),

                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"] = "Authenticate the user",
                            ["profile"] = "Read user profile",
                            ["email"] = "Read user email"
                        }
                    }
                }
            }
        };

        foreach (var operation in document.Paths.Values
            .SelectMany(path => path.Operations))
        {
            operation.Value.Security ??= [];

            operation.Value.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(
                        "OAuth2",
                        document)] = []
                });
        }
    }
}