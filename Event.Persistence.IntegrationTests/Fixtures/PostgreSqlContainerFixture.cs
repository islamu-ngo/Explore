using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Event.Persistence.IntegrationTests.Fixtures;

public class PostgreSqlContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("explore_db_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            // Ensure Docker socket is accessible or configure for environment
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    public ExploreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        var context = new ExploreDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
