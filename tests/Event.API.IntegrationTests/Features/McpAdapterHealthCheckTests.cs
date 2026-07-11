// ABOUTME: Unit-style tests for the optional MCP adapter readiness health check.
// ABOUTME: Verifies startup/runtime effective posture reports safe bounded configuration only.

using Explore.API.Configuration;
using Explore.API.HealthChecks;
using Explore.API.Mcp;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpAdapterHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncWhenMcpStartupDisabledReturnsDegradedWithSafeData()
    {
        var healthCheck = CreateHealthCheck(new McpRuntimeState(
            StartupEnabled: false,
            RuntimeEnabled: true,
            EffectiveEnabled: false,
            StartupLegacySseCeiling: false,
            RuntimeLegacySseRequested: false,
            LegacySseRuntimeEnabled: false,
            TenantOverrideAllowed: false,
            TenantLegacySseOverrideAllowed: false,
            TenantId: null));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("startup");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("startupEnabled").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("runtimeEnabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("endpointPath").WhoseValue.Should().Be("/mcp");
        result.Data.Should().ContainKey("stateless").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("legacySseStartupCeiling").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("legacySseRuntimeRequested").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("legacySseRuntimeEnabled").WhoseValue.Should().Be(false);
        AssertSafeKeys(result);
    }

    [Test]
    public async Task CheckHealthAsyncWhenMcpRuntimeDisabledReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(new McpRuntimeState(
            StartupEnabled: true,
            RuntimeEnabled: false,
            EffectiveEnabled: false,
            StartupLegacySseCeiling: false,
            RuntimeLegacySseRequested: false,
            LegacySseRuntimeEnabled: false,
            TenantOverrideAllowed: false,
            TenantLegacySseOverrideAllowed: false,
            TenantId: null));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("runtime");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("startupEnabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("runtimeEnabled").WhoseValue.Should().Be(false);
        AssertSafeKeys(result);
    }

    [Test]
    public async Task CheckHealthAsyncWhenMcpEnabledReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck(new McpRuntimeState(
            StartupEnabled: true,
            RuntimeEnabled: true,
            EffectiveEnabled: true,
            StartupLegacySseCeiling: true,
            RuntimeLegacySseRequested: true,
            LegacySseRuntimeEnabled: false,
            TenantOverrideAllowed: false,
            TenantLegacySseOverrideAllowed: false,
            TenantId: null));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("stateless Streamable HTTP");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("legacySseRuntimeRequested").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("legacySseRuntimeEnabled").WhoseValue.Should().Be(false);
        AssertSafeKeys(result);
    }

    private static McpAdapterHealthCheck CreateHealthCheck(McpRuntimeState state)
    {
        var runtimeStateService = Substitute.For<IMcpRuntimeStateService>();
        runtimeStateService.GetAsync(null, Arg.Any<CancellationToken>()).Returns(state);

        var services = new ServiceCollection();
        services.AddScoped(_ => runtimeStateService);
        var provider = services.BuildServiceProvider();

        return new McpAdapterHealthCheck(
            Options.Create(new McpAdapterSettings
            {
                Enabled = state.StartupEnabled,
                EndpointPath = "/mcp",
                Stateless = true,
                EnableLegacySse = state.StartupLegacySseCeiling
            }),
            provider.GetRequiredService<IServiceScopeFactory>());
    }

    private static void AssertSafeKeys(HealthCheckResult result)
    {
        result.Data.Keys.Should().NotContain(key => key.Contains("tenant", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("prompt", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("payload", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("apiKey", StringComparison.OrdinalIgnoreCase));
    }
}
