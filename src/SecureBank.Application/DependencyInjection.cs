using FluentValidation;
using Mapster;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using SecureBank.Application.Abstractions.Mapping;
using SecureBank.Application.Behaviors;

namespace SecureBank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        TypeAdapterConfig.GlobalSettings.Scan(
            typeof(MappingConfiguration).Assembly);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(AuthorizationBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(TransactionalBehavior<,>));

        return services;
    }
}
