// ABOUTME: Readiness health check for the optional API-hosted MCP adapter posture.
// ABOUTME: Reports bounded configuration only without tenant, prompt, payload, or provider data.

using Explore.API.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.API.HealthChecks;

public sealed class McpAdapterHealthCheck(IOptions<McpAdapterSettings> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var data = new Dictionary<string, object>
        {
            ["enabled"] = settings.Enabled,
            ["endpointPath"] = settings.EndpointPath,
            ["stateless"] = settings.Stateless,
            ["legacySseEnabled"] = settings.EnableLegacySse
        };

        if (!settings.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "MCP adapter is intentionally disabled.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "MCP adapter is enabled with stateless Streamable HTTP transport.",
            data));
    }
}
