// ABOUTME: Readiness health check for the LocalProvider webhook delivery queue.
// ABOUTME: Reports queue backlog and stale sending leases without exposing endpoints, payloads, or secrets.

using Explore.Application.Contracts.Persistence;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.HealthChecks;

public sealed class LocalWebhookDeliveryHealthCheck(
    IOptions<WebhookDeliveryProcessorSettings> settings,
    IOptionsMonitor<WebhookOptions> webhookOptions,
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var processorSettings = settings.Value;
        var options = webhookOptions.CurrentValue;
        var localProviderSelected = !options.IsDisabled
            && (options.IsProvider(WebhookOptions.ProviderLocal)
                || options.IsProvider(WebhookOptions.ProviderComposite));
        var data = new Dictionary<string, object>
        {
            ["enabled"] = options.Enabled,
            ["provider"] = options.Provider,
            ["localProviderSelected"] = localProviderSelected,
            ["processorEnabled"] = processorSettings.Enabled,
            ["batchSize"] = processorSettings.BatchSize,
            ["processingLeaseTimeoutSeconds"] = processorSettings.ProcessingLeaseTimeoutSeconds,
            ["dueAttemptWarningThreshold"] = processorSettings.HealthDueAttemptWarningThreshold,
            ["staleSendingWarningThreshold"] = processorSettings.HealthStaleSendingWarningThreshold
        };

        if (!localProviderSelected)
        {
            return HealthCheckResult.Healthy(
                "Local webhook delivery is not the selected outgoing provider.",
                data: data);
        }

        if (!processorSettings.Enabled)
        {
            return HealthCheckResult.Degraded(
                "Local webhook delivery processor is disabled.",
                data: data);
        }

        var now = DateTime.UtcNow;
        var processingStartedBefore = now.AddSeconds(-processorSettings.ProcessingLeaseTimeoutSeconds);
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var dueScheduledAttempts = await attemptRepository.CountDueScheduledAsync(now, cancellationToken);
        var staleSendingAttempts = await attemptRepository.CountStaleSendingAsync(
            processingStartedBefore,
            cancellationToken);

        data["dueScheduledAttempts"] = dueScheduledAttempts;
        data["staleSendingAttempts"] = staleSendingAttempts;
        data["processingStartedBefore"] = processingStartedBefore;

        if (staleSendingAttempts >= processorSettings.HealthStaleSendingWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                "Local webhook delivery has stale sending attempts.",
                data: data);
        }

        if (dueScheduledAttempts >= processorSettings.HealthDueAttemptWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                "Local webhook delivery due-attempt backlog is above the configured threshold.",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "Local webhook delivery queue is healthy.",
            data: data);
    }
}
