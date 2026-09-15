using Microsoft.Extensions.DependencyInjection;

namespace SecureBank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediator();

        return services;
    }
}