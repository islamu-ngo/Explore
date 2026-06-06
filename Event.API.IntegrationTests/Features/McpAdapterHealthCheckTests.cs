// ABOUTME: Unit-style tests for the optional MCP adapter readiness health check.
// ABOUTME: Verifies disabled/enabled posture reports safe bounded configuration only.

using Explore.API.Configuration;
using Explore.API.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpAdapterHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncWhenMcpDisabledReturnsDegradedWithSafeData()
    {
        var healthCheck = new McpAdapterHealthCheck(Options.Create(new McpAdapterSettings
        {
            Enabled = false,
            EndpointPath = "/mcp",
            Stateless = true,
            EnableLegacySse = false
        }));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("endpointPath").WhoseValue.Should().Be("/mcp");
        result.Data.Should().ContainKey("stateless").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("legacySseEnabled").WhoseValue.Should().Be(false);
        result.Data.Keys.Should().NotContain(key => key.Contains("tenant", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("prompt", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("payload", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("apiKey", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CheckHealthAsyncWhenMcpEnabledReturnsHealthy()
    {
        var healthCheck = new McpAdapterHealthCheck(Options.Create(new McpAdapterSettings
        {
            Enabled = true,
            EndpointPath = "/mcp",
            Stateless = true,
            EnableLegacySse = false
        }));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("stateless Streamable HTTP");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
    }
}
