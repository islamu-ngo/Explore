// ABOUTME: Hosted worker that applies Explore database migrations, model-owned PostgreSQL constraints, and seed data.
// ABOUTME: Runs once in the migration service process before stopping the host.

using Explore.Application.Configuration;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Persistence.Schema;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Event.MigrationService;

public sealed class Worker(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, IHostEnvironment environment, IConfiguration configuration, IOptions<PrivacyErasureDurabilityOptions> erasureOptions, ILogger<Worker> logger) : BackgroundService
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
        logger.LogInformation("Starting database migration...");

        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var dataProtectionDb = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();

        // Apply migrations
        logger.LogInformation("Applying database migrations...");
        await ExploreDatabaseMigrator.MigrateAsync(db, configuration, stoppingToken);
        logger.LogInformation("Database migrations applied successfully.");

        await SqliteDatabaseInitializer.InitializeAsync(db, stoppingToken);

        if (db.Database.IsNpgsql())
        {
            logger.LogInformation("Applying model-owned PostgreSQL constraints...");
            await PostgresModelConstraintApplier.ApplyAsync(db, stoppingToken);
            logger.LogInformation("Model-owned PostgreSQL constraints applied successfully.");
        }

        logger.LogInformation("Applying Data Protection key-ring migrations...");
        await dataProtectionDb.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Data Protection key-ring migrations applied successfully.");

        if (erasureOptions.Value.Topology == PrivacyErasureAuthorityTopology.ExternalDatabase)
        {
            logger.LogInformation("Applying external privacy-erasure authority migrations...");
            var authorityDb = scope.ServiceProvider.GetRequiredService<PrivacyErasureAuthorityDbContext>();
            await authorityDb.Database.MigrateAsync(stoppingToken);
            logger.LogInformation("External privacy-erasure authority migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Applying embedded privacy-erasure authority migrations...");
            var storage = scope.ServiceProvider.GetRequiredService<EmbeddedPrivacyErasureAuthorityStorage>();
            await storage.EnsureReadyAsync(stoppingToken);
            var authorityDb = scope.ServiceProvider
                .GetRequiredService<EmbeddedPrivacyErasureAuthorityDbContext>();
            await authorityDb.Database.MigrateAsync(stoppingToken);
            storage.HardenCompanionFiles();
            await storage.VerifyIntegrityAsync(stoppingToken);
            logger.LogInformation("Embedded privacy-erasure authority migrations applied successfully.");
        }

        // Run async seeding for data that requires conditional logic
        logger.LogInformation("Running database seeding...");
        await DatabaseSeeder.SeedAsync(db, environment, configuration: configuration, cancellationToken: stoppingToken);
        logger.LogInformation("Database seeding completed successfully.");

        lifetime.StopApplication();
    }
}
