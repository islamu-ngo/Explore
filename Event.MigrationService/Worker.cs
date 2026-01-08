using Explore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Event.MigrationService;

public sealed class Worker(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        await db.Database.MigrateAsync(stoppingToken);

        // optional: seed here, if you want

        lifetime.StopApplication();
    }
}
