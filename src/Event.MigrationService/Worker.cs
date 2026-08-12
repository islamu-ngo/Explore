// ABOUTME: Hosted worker that applies Explore database migrations, model-owned PostgreSQL constraints, and seed data.
// ABOUTME: Runs once in the migration service process before stopping the host.

using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Secrets.Database;

namespace Event.MigrationService;

public sealed class Worker(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, IHostEnvironment environment, IConfiguration configuration, PrimaryDatabaseConnectionOptions migrationDatabaseOptions, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await MigrateAsync(stoppingToken);
        }
        catch
        {
            Environment.ExitCode = 1;
            throw;
        }
    }

    private async Task MigrateAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        await ExploreDatabaseMigrator.MigrateAndSeedAsync(
            db,
            environment,
            configuration,
            migrationDatabaseOptions,
            logger,
            stoppingToken);

        lifetime.StopApplication();
    }
}
