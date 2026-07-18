// ABOUTME: Readiness health check for Basic Dispatch Mode email dispatch scheduler and outbox state.
// ABOUTME: Reports backlog, retry, stale-processing, and dead-letter signals without exposing message content.

using Explore.API.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Telemetry;
using Explore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.API.HealthChecks;

public sealed class EmailDispatchHealthCheck(
    IOptions<EmailDispatchProcessorSettings> options,
    IOptions<TickerQSchedulerOptions> schedulerOptions,
    IServiceScopeFactory scopeFactory,
    BusinessMetrics metrics) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var scheduler = schedulerOptions.Value;
        var data = new Dictionary<string, object>
        {
            ["enabled"] = settings.Enabled,
            ["mode"] = settings.Mode.ToString(),
            ["pollingIntervalSeconds"] = settings.PollingIntervalSeconds,
            ["batchSize"] = settings.BatchSize,
            ["maxAttemptCount"] = settings.MaxAttemptCount,
            ["processingLeaseTimeoutSeconds"] = settings.ProcessingLeaseTimeoutSeconds,
            ["dueDispatchWarningThreshold"] = settings.HealthDueDispatchWarningThreshold,
            ["staleProcessingWarningThreshold"] = settings.HealthStaleProcessingWarningThreshold,
            ["deadLetterWarningThreshold"] = settings.HealthDeadLetterWarningThreshold,
            ["oldestPendingWarningSeconds"] = settings.HealthOldestPendingWarningSeconds,
            ["tenantBacklogWarningThreshold"] = settings.HealthTenantBacklogWarningThreshold,
            ["consumerId"] = settings.ConsumerId,
            ["tickerQEnabled"] = scheduler.Enabled,
            ["tickerQDashboardEnabled"] = scheduler.DashboardEnabled
        };

        if (!settings.Enabled)
        {
            return HealthCheckResult.Degraded(
                "Basic email dispatch is intentionally disabled.",
                data: data);
        }

        if (settings.Mode == EmailDispatchProcessorMode.Disabled)
        {
            return HealthCheckResult.Degraded(
                "Basic email dispatch scheduler mode is Disabled.",
                data: data);
        }

        if (settings.Mode == EmailDispatchProcessorMode.TickerQ && !scheduler.Enabled)
        {
            return HealthCheckResult.Unhealthy(
                "Basic email dispatch is configured for TickerQ, but TickerQ scheduler is disabled.",
                data: data);
        }

        var now = DateTime.UtcNow;
        var processingStartedBefore = now.AddSeconds(-settings.ProcessingLeaseTimeoutSeconds);
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var dueDispatchCount = await repository.CountDueDispatchAsync(now, cancellationToken);
        var retryScheduledCount = await repository.CountRetryScheduledAsync(cancellationToken);
        var staleProcessingCount = await repository.CountStaleProcessingAsync(
            processingStartedBefore,
            cancellationToken);
        var deadLetteredCount = await repository.CountDeadLetteredAsync(cancellationToken);
        var oldestDueCreatedAt = await repository.GetOldestDueCreatedAtAsync(now, cancellationToken);
        var tenantBacklog = await repository.CountDueDispatchByTenantAsync(
            now,
            settings.HealthTenantSampleLimit,
            cancellationToken);
        var oldestPendingAgeSeconds = oldestDueCreatedAt is null
            ? 0d
            : Math.Max(0d, (now - oldestDueCreatedAt.Value).TotalSeconds);

        data["dueDispatchCount"] = dueDispatchCount;
        data["retryScheduledCount"] = retryScheduledCount;
        data["staleProcessingCount"] = staleProcessingCount;
        data["deadLetteredCount"] = deadLetteredCount;
        data["processingStartedBefore"] = processingStartedBefore;
        data["oldestPendingAgeSeconds"] = oldestPendingAgeSeconds;
        data["tenantBacklogSample"] = tenantBacklog;
        metrics.RecordEmailDispatchOldestPendingAge(oldestPendingAgeSeconds);
        foreach (var (tenantId, count) in tenantBacklog)
        {
            metrics.RecordEmailDispatchTenantBacklog(tenantId.ToString(), count);
        }

        if (staleProcessingCount >= settings.HealthStaleProcessingWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                "Basic email dispatch has stale processing rows.",
                data: data);
        }

        if (deadLetteredCount >= settings.HealthDeadLetterWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                "Basic email dispatch has dead-lettered rows.",
                data: data);
        }

        if (dueDispatchCount >= settings.HealthDueDispatchWarningThreshold)
        {
            return HealthCheckResult.Degraded(
                "Basic email dispatch due backlog is above the configured threshold.",
                data: data);
        }

        if (oldestPendingAgeSeconds >= settings.HealthOldestPendingWarningSeconds)
        {
            return HealthCheckResult.Degraded(
                "Basic email dispatch oldest pending row is above the configured age threshold.",
                data: data);
        }

        if (tenantBacklog.Values.Any(count => count >= settings.HealthTenantBacklogWarningThreshold))
        {
            return HealthCheckResult.Degraded(
                "Basic email dispatch tenant backlog is above the configured threshold.",
                data: data);
        }

        return HealthCheckResult.Healthy(
            settings.Mode == EmailDispatchProcessorMode.TickerQ
                ? "Basic email dispatch is scheduled by TickerQ."
                : "Basic email dispatch hosted service is enabled.",
            data);
    }
}
