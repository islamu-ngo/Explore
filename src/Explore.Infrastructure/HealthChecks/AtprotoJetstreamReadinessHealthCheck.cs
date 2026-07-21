// ABOUTME: Reports whether capability-aware ATProto Jetstream ingestion can safely become active.
// ABOUTME: Reports public-collection or DID-curated readiness without exposing source identities.

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

        return options.Value.AllowedDids is { Length: > 0 }
            ? HealthCheckResult.Healthy(
                "ATProto Jetstream capability and curated DID filter are ready.",
                data)
            : HealthCheckResult.Healthy(
                "ATProto Jetstream capability and public collection subscription are ready.",
                data);
    }
}
