using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SecureBank.Infrastructure.Persistence;

internal sealed class MigrationHostedService(
    IServiceProvider serviceProvider,
    IHostEnvironment environment)
    : IHostedService
{
    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<SecureBankDbContext>();

        await dbContext.Database.MigrateAsync(
            cancellationToken);

        if (environment.IsDevelopment())
        {
            await DevelopmentDataSeeder.SeedAsync(
                dbContext,
                cancellationToken);
        }
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
