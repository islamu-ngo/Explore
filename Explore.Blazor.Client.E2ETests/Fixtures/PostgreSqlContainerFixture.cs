// ABOUTME: PostgreSQL Testcontainers fixture for isolated browser E2E application stacks.
// ABOUTME: Provisions external storage while the API remains responsible for schema and seed ownership.

using Testcontainers.PostgreSql;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class PostgreSqlContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("explore_e2e_test")
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
