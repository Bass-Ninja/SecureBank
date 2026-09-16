using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureBank.Application.Abstractions;
using SecureBank.Infrastructure.Authentication;
using SecureBank.Infrastructure.Behaviours;
using SecureBank.Infrastructure.Documentation;
using SecureBank.Infrastructure.Persistence;

namespace SecureBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddAuthentication(configuration)
            .AddDocumentation();

        services
            .AddExceptionHandler<GlobalExceptionHandler>()
            .AddProblemDetails();
        
        services.AddHostedService<MigrationHostedService>();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is not configured.");

        services.AddDbContext<SecureBankDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ISecureBankDbContext>(
            provider =>
                provider.GetRequiredService<SecureBankDbContext>());

        return services;
    }

    private static IServiceCollection AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IUserContext, UserContext>();

        services.AddKeycloakAuthentication(configuration);

        return services;
    }
    
    private static IServiceCollection AddDocumentation(
        this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<OAuthSecuritySchemeTransformer>();
            options.AddSchemaTransformer<NullableEnumSchemaTransformer>();
        });

        return services;
    }
    
    public static WebApplication UseDocumentation(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    "/openapi/v1.json",
                    "SecureBank API v1");

                options.OAuthClientId("securebank-swagger");
                options.OAuthAppName("SecureBank Swagger");
                options.OAuthScopes(
                    "openid",
                    "profile",
                    "email");
                options.OAuthUsePkce();
            });
        }

        return app;
    }
}
