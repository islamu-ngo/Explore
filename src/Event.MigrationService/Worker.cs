// ABOUTME: Hosted worker that applies Explore database migrations, model-owned PostgreSQL constraints, and seed data.
// ABOUTME: Runs once in the migration service process before stopping the host.

using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Event.MigrationService;

public sealed class Worker(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, IHostEnvironment environment, IConfiguration configuration, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting database migration...");

        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var dataProtectionDb = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();

        // Apply migrations
        logger.LogInformation("Applying database migrations...");
        await EventLocationPrivacyMigrationStage.MigrateAsync(
            db,
            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey],
            stoppingToken);
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Applying model-owned PostgreSQL constraints...");
        await PostgresModelConstraintApplier.ApplyAsync(db, stoppingToken);
        logger.LogInformation("Model-owned PostgreSQL constraints applied successfully.");

        logger.LogInformation("Applying Data Protection key-ring migrations...");
        await dataProtectionDb.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Data Protection key-ring migrations applied successfully.");

        // Run async seeding for data that requires conditional logic
        logger.LogInformation("Running database seeding...");
        await DatabaseSeeder.SeedAsync(db, environment, configuration: configuration, cancellationToken: stoppingToken);
        logger.LogInformation("Database seeding completed successfully.");

        lifetime.StopApplication();
    }
}
