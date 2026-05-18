// ABOUTME: Non-blocking API hosted service that triggers one Cerbos policy package publish after startup.
// ABOUTME: Delegates to CerbosPolicyBootSyncRunner so startup remains resilient when Cerbos is unavailable.

using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

/// <summary>
/// Background hosted service that performs zero-touch Cerbos policy package publishing once per process boot.
/// </summary>
public sealed class CerbosPolicyBootSyncWorker(
    CerbosPolicyBootSyncRunner runner,
    IOptions<CerbosPolicyBootSyncOptions> options,
    ILogger<CerbosPolicyBootSyncWorker> logger) : BackgroundService
{
    private readonly CerbosPolicyBootSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var initialDelay = TimeSpan.FromSeconds(Math.Max(0, _options.InitialDelaySeconds));
            if (initialDelay > TimeSpan.Zero)
            {
                await Task.Delay(initialDelay, stoppingToken).ConfigureAwait(false);
            }

            await runner.RunOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Cerbos policy boot sync worker stopped before publishing");
        }
    }
}
