using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecureBank.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

            });

        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        return services;
    }
}
