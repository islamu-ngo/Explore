using Explore.Persistence;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Event.MigrationService;

public sealed class Worker(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting database migration...");
        
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        // Apply migrations
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Database migrations applied successfully.");

        // Run async seeding for data that requires conditional logic
        logger.LogInformation("Running database seeding...");
        await DatabaseSeeder.SeedAsync(db, stoppingToken);
        logger.LogInformation("Database seeding completed successfully.");

        lifetime.StopApplication();
    }
}
