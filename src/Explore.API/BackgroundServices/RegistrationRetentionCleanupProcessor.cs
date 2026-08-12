// ABOUTME: Runs bounded registration answer and PII retention cleanup for active tenants.
// ABOUTME: Keeps scheduling in API while immutable-deadline deletion remains in Persistence.

using Explore.Application.Contracts.Persistence;

namespace Explore.API.BackgroundServices;

public sealed class RegistrationRetentionCleanupProcessor(
    IServiceProvider serviceProvider,
    ILogger<RegistrationRetentionCleanupProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
                var cleanup = scope.ServiceProvider.GetRequiredService<IRegistrationRetentionCleanupRepository>();
                int deleted = 0;
                foreach (var tenant in await tenants.GetActiveAsNoTrackingAsync(stoppingToken))
                {
                    deleted += (await cleanup.CleanupTenantAsync(
                        tenant.Id, DateTime.UtcNow, 500, stoppingToken)).TotalDeleted;
                }

                logger.LogInformation("Registration retention cleanup completed. DeletedRows={DeletedRows}", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Registration retention cleanup failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
