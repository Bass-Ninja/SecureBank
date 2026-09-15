using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureBank.Infrastructure.Persistence;

namespace SecureBank.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static async Task InitializeInfrastructureAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<SecureBankDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await DevelopmentDataSeeder.SeedAsync(
            dbContext,
            cancellationToken);
    }
}