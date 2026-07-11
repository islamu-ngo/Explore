// ABOUTME: Readiness health check for the Blazor BFF Data Protection key store.
// ABOUTME: Verifies the Redis key-ring store is reachable without exposing key material.

using Explore.Blazor.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Explore.Blazor.HealthChecks;

public sealed class DataProtectionKeyStoreHealthCheck(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<DataProtectionKeyStoreHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = connectionMultiplexer.GetDatabase();
            var latency = await database.PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var keyRingPresent = await database
                .KeyExistsAsync(BffDataProtectionExtensions.KeyRingName)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy(
                "Data Protection key store is reachable.",
                new Dictionary<string, object>
                {
                    ["keyRingPresent"] = keyRingPresent,
                    ["latencyMilliseconds"] = latency.TotalMilliseconds,
                    ["store"] = "redis"
                });
        }
        catch (Exception ex)
        {
            var failureType = ex.GetType().Name;
            logger.LogWarning("Data Protection key store health check failed with {FailureType}.", failureType);

            return HealthCheckResult.Unhealthy(
                "Data Protection key store is unreachable.",
                ex,
                new Dictionary<string, object>
                {
                    ["failureType"] = failureType,
                    ["store"] = "redis"
                });
        }
    }
}
