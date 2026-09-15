using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureBank.Application.Abstractions;
using SecureBank.Infrastructure.Authentication;
using SecureBank.Infrastructure.Persistence;

namespace SecureBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SecureBankDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ISecureBankDbContext>(
            provider => provider.GetRequiredService<SecureBankDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        services.AddKeycloakAuthentication(configuration);

        return services;
    }
}