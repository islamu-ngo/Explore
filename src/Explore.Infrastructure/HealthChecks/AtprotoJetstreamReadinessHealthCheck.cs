// ABOUTME: Reports whether capability-aware ATProto Jetstream ingestion can safely become active.
// ABOUTME: Keeps dormant federation healthy while exposing bounded readiness when curated DIDs are missing.

using Explore.Infrastructure.Services.Federation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.HealthChecks;

public sealed class AtprotoJetstreamReadinessHealthCheck(
    IAtprotoJetstreamRuntimeStore store,
    IOptions<AtprotoJetstreamOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> enabledTenants;
        try
        {
            enabledTenants = await store.ResolveEnabledTenantIdsAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "ATProto Jetstream capability readiness could not be resolved; ingestion remains stopped.");
        }

        var data = new Dictionary<string, object>
        {
            ["capabilityEnabled"] = enabledTenants.Count > 0,
            ["allowlistConfigured"] = options.Value.AllowedDids is { Length: > 0 }
        };
        if (enabledTenants.Count == 0)
        {
            return HealthCheckResult.Healthy(
                "ATProto Jetstream ingestion is dormant because no tenant capability is enabled.",
                data);
        }

        if (options.Value.AllowedDids is not { Length: > 0 })
        {
            return HealthCheckResult.Unhealthy(
                "ATProto Jetstream is enabled but no curated DID allowlist is configured; ingestion remains stopped.",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "ATProto Jetstream capability and curated DID allowlist are ready.",
            data);
    }
}
