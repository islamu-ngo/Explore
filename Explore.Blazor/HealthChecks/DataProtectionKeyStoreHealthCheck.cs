// ABOUTME: Readiness health check for the Blazor BFF Data Protection key store.
// ABOUTME: Verifies the persisted key-ring table is reachable without exposing key material.

using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Explore.Blazor.HealthChecks;

public sealed class DataProtectionKeyStoreHealthCheck(
    IServiceScopeFactory scopeFactory,
    ILogger<DataProtectionKeyStoreHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();
            var keyCount = await db.DataProtectionKeys.CountAsync(cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy(
                "Data Protection key store is reachable.",
                new Dictionary<string, object>
                {
                    ["keyCount"] = keyCount,
                    ["store"] = "database"
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
                    ["store"] = "database"
                });
        }
    }
}
