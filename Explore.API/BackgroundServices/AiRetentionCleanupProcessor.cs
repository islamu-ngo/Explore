// ABOUTME: Hosted worker that periodically runs tenant-scoped AI retention cleanup.
// ABOUTME: Keeps scheduling separate from redaction logic and logs only safe aggregate counts.

using Explore.Application.Contracts.Services;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class AiRetentionCleanupProcessor(
    IServiceProvider serviceProvider,
    IOptions<AiRetentionCleanupSettings> options,
    ILogger<AiRetentionCleanupProcessor> logger) : BackgroundService
{
    private readonly AiRetentionCleanupSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("AI retention cleanup processor is disabled.");
            return;
        }

        logger.LogInformation(
            "AI retention cleanup processor starting. InitialDelaySeconds={InitialDelaySeconds}, PollingIntervalMinutes={PollingIntervalMinutes}, DryRun={DryRun}, MaxTenantsPerPass={MaxTenantsPerPass}.",
            _settings.InitialDelaySeconds,
            _settings.PollingIntervalMinutes,
            _settings.DryRun,
            _settings.MaxTenantsPerPass);

        if (_settings.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.InitialDelaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "AI retention cleanup pass failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.PollingIntervalMinutes), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IAiRetentionCleanupService>();
        var result = await cleanupService.CleanupAllTenantsAsync(DateTime.UtcNow, cancellationToken);

        logger.LogInformation(
            "AI retention cleanup pass completed. Tenants={TenantCount}, SucceededTenants={SucceededTenantCount}, FailedTenants={FailedTenantCount}, Eligible={EligibleConversations}, Redacted={RedactedConversations}, DryRun={DryRun}.",
            result.TenantCount,
            result.SucceededTenantCount,
            result.FailedTenantCount,
            result.EligibleConversations,
            result.RedactedConversations,
            result.DryRun);
    }
}
