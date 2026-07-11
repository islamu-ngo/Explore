// ABOUTME: Unit-style tests for the API IdempotencyCleanupHealthCheck.
// ABOUTME: Verifies cleanup readiness reports enabled, dry-run, and disabled states safely.

using Explore.API.HealthChecks;
using Explore.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class IdempotencyCleanupHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncWhenCleanupEnabledReturnsHealthyWithSafeData()
    {
        var options = Options.Create(new IdempotencyCleanupSettings
        {
            Enabled = true,
            DryRun = false,
            InitialDelaySeconds = 5,
            PollingIntervalMinutes = 15,
            BatchSize = 25,
            ExpirationGraceHours = 12
        });
        var healthCheck = new IdempotencyCleanupHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("delete mode");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("dryRun").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("initialDelaySeconds").WhoseValue.Should().Be(5);
        result.Data.Should().ContainKey("pollingIntervalMinutes").WhoseValue.Should().Be(15);
        result.Data.Should().ContainKey("batchSize").WhoseValue.Should().Be(25);
        result.Data.Should().ContainKey("expirationGraceHours").WhoseValue.Should().Be(12);
        result.Data.Keys.Should().NotContain(key => key.Contains("key", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("tenant", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("path", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CheckHealthAsyncWhenCleanupDryRunReturnsHealthy()
    {
        var options = Options.Create(new IdempotencyCleanupSettings
        {
            Enabled = true,
            DryRun = true
        });
        var healthCheck = new IdempotencyCleanupHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("dry-run mode");
        result.Data.Should().ContainKey("dryRun").WhoseValue.Should().Be(true);
    }

    [Test]
    public async Task CheckHealthAsyncWhenCleanupDisabledReturnsDegraded()
    {
        var options = Options.Create(new IdempotencyCleanupSettings
        {
            Enabled = false
        });
        var healthCheck = new IdempotencyCleanupHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("intentionally disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
    }
}
