// ABOUTME: Readiness health check for Basic Dispatch Mode email dispatch worker configuration.
// ABOUTME: Makes enabled/disabled dispatch status explicit for self-hosting operators.

using Explore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.API.HealthChecks;

public sealed class EmailDispatchHealthCheck(IOptions<EmailDispatchProcessorSettings> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var data = new Dictionary<string, object>
        {
            ["enabled"] = settings.Enabled,
            ["pollingIntervalSeconds"] = settings.PollingIntervalSeconds,
            ["batchSize"] = settings.BatchSize,
            ["maxAttemptCount"] = settings.MaxAttemptCount,
            ["consumerId"] = settings.ConsumerId
        };

        if (!settings.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Basic email dispatch is intentionally disabled.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Basic email dispatch processor is enabled.",
            data));
    }
}
