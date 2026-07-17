// ABOUTME: Isolated PostgreSQL Testcontainer fixture for recipient-delivery migration verification.
// ABOUTME: Never reads application configuration and exposes only its container-owned connection string.

using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Event.Persistence.IntegrationTests.Fixtures;

public sealed class RecipientDeliveryMigrationContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("recipient_delivery_migration_" + Guid.NewGuid().ToString("N"))
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
