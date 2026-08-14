// ABOUTME: Hosted organizer payment readiness reconciliation loop for stale provider connections.
// ABOUTME: Creates a fresh scope per bounded batch and logs only safe aggregate counts and provider request IDs.

using Explore.Application.Features.OrganizerPaymentConnections;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class OrganizerPaymentReadinessReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OrganizerPaymentReadinessReconciliationOptions> options,
    ILogger<OrganizerPaymentReadinessReconciliationWorker> logger) : BackgroundService
{
    private readonly OrganizerPaymentReadinessReconciliationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Organizer payment readiness reconciliation worker is disabled");
            return;
        }

        if (_options.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.InitialDelaySeconds), stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollingIntervalSeconds));
        do
        {
            try
            {
                OrganizerPaymentReadinessReconciliationResult result = await RunOnceAsync(stoppingToken);
                if (result.ProcessedCount > 0 || result.FailureCount > 0)
                {
                    logger.LogInformation(
                        "Organizer payment readiness reconciliation processed {ProcessedCount}/{DueCount}; updated {UpdatedCount}, skipped {SkippedCount}, failures {FailureCount}; failure samples {FailureSamples}",
                        result.ProcessedCount,
                        result.DueCount,
                        result.UpdatedCount,
                        result.SkippedCount,
                        result.FailureCount,
                        result.Failures.Select(failure => new { failure.FailureCode, failure.ProviderRequestId }).ToArray());
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Organizer payment readiness reconciliation cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task<OrganizerPaymentReadinessReconciliationResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<OrganizerPaymentReadinessReconciliationService>();
        return await service.ReconcileOnceAsync(cancellationToken);
    }
}
