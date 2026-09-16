using Microsoft.EntityFrameworkCore;
using SecureBank.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SecureBank.IntegrationTests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18")
            .WithDatabase("securebank_test")
            .WithUsername("securebank")
            .WithPassword("securebank_test_password")
            .Build();

    public SecureBankDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SecureBankDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new SecureBankDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
