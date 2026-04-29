using Explore.Persistence;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Event.MigrationService;

public sealed class Worker(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, IHostEnvironment environment, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting database migration...");

        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var dataProtectionDb = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();

        // Apply migrations
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Applying Data Protection key-ring migrations...");
        await dataProtectionDb.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Data Protection key-ring migrations applied successfully.");

        // Run async seeding for data that requires conditional logic
        logger.LogInformation("Running database seeding...");
        await DatabaseSeeder.SeedAsync(db, environment, stoppingToken);
        logger.LogInformation("Database seeding completed successfully.");

        lifetime.StopApplication();
    }
}
