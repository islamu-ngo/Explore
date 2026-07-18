// ABOUTME: Hosted timer that triggers bounded email dispatch content redaction.
// ABOUTME: Keeps scheduling in the API host while scoped cleanup logic lives in Infrastructure.

using Explore.Application.Contracts.Services;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class EmailDispatchRetentionCleanupProcessor(
    IServiceProvider serviceProvider,
    IOptions<EmailDispatchRetentionSettings> settings,
    ILogger<EmailDispatchRetentionCleanupProcessor> logger) : BackgroundService
{
    private readonly EmailDispatchRetentionSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Email dispatch retention cleanup processor is disabled");
            return;
        }

        if (_settings.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.InitialDelaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IEmailDispatchRetentionCleanupService>();
                await cleanupService.CleanupAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error in email dispatch retention cleanup processor loop");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.PollingIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
