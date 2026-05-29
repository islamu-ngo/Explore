// ABOUTME: Readiness health check for Basic Dispatch Mode email dispatch scheduler configuration.
// ABOUTME: Makes selected TickerQ, hosted-service, or disabled dispatch mode explicit for operators.

using Explore.API.Configuration;
using Explore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.API.HealthChecks;

public sealed class EmailDispatchHealthCheck(
    IOptions<EmailDispatchProcessorSettings> options,
    IOptions<TickerQSchedulerOptions> schedulerOptions) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
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
            ["consumerId"] = settings.ConsumerId,
            ["tickerQEnabled"] = scheduler.Enabled,
            ["tickerQDashboardEnabled"] = scheduler.DashboardEnabled
        };

        if (!settings.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Basic email dispatch is intentionally disabled.",
                data: data));
        }

        if (settings.Mode == EmailDispatchProcessorMode.Disabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Basic email dispatch scheduler mode is Disabled.",
                data: data));
        }

        if (settings.Mode == EmailDispatchProcessorMode.TickerQ && !scheduler.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Basic email dispatch is configured for TickerQ, but TickerQ scheduler is disabled.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            settings.Mode == EmailDispatchProcessorMode.TickerQ
                ? "Basic email dispatch is scheduled by TickerQ."
                : "Basic email dispatch hosted service is enabled.",
            data));
    }
}
