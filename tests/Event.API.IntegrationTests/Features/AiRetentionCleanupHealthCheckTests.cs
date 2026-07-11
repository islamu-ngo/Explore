// ABOUTME: Unit-style tests for the API AiRetentionCleanupHealthCheck.
// ABOUTME: Verifies scheduled AI cleanup readiness reports safe bounded configuration data.

using Explore.API.HealthChecks;
using Explore.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class AiRetentionCleanupHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncWhenCleanupEnabledReturnsHealthyWithSafeData()
    {
        var options = Options.Create(new AiRetentionCleanupSettings
        {
            Enabled = true,
            DryRun = false,
            InitialDelaySeconds = 5,
            PollingIntervalMinutes = 15,
            MaxTenantsPerPass = 25
        });
        var healthCheck = new AiRetentionCleanupHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("redaction mode");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("dryRun").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("initialDelaySeconds").WhoseValue.Should().Be(5);
        result.Data.Should().ContainKey("pollingIntervalMinutes").WhoseValue.Should().Be(15);
        result.Data.Should().ContainKey("maxTenantsPerPass").WhoseValue.Should().Be(25);
        result.Data.Keys.Should().NotContain(key => key.Equals("tenantId", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Equals("tenantSlug", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("prompt", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("payload", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CheckHealthAsyncWhenCleanupDryRunReturnsHealthy()
    {
        var options = Options.Create(new AiRetentionCleanupSettings
        {
            Enabled = true,
            DryRun = true
        });
        var healthCheck = new AiRetentionCleanupHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("dry-run mode");
        result.Data.Should().ContainKey("dryRun").WhoseValue.Should().Be(true);
    }

    [Test]
    public async Task CheckHealthAsyncWhenCleanupDisabledReturnsDegraded()
    {
        var options = Options.Create(new AiRetentionCleanupSettings
        {
            Enabled = false
        });
        var healthCheck = new AiRetentionCleanupHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("intentionally disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
    }
}
