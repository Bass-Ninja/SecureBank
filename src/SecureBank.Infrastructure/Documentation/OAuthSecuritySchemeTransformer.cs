using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace SecureBank.Infrastructure.Documentation;

public sealed class OAuthSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
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
                        AuthorizationUrl =
                            new Uri(
                                "http://localhost:8081/realms/securebank/protocol/openid-connect/auth"),

                        TokenUrl =
                            new Uri(
                                "http://localhost:8081/realms/securebank/protocol/openid-connect/token"),

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
