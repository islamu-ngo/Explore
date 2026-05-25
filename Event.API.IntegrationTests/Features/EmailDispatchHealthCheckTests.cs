// ABOUTME: Unit-style tests for the API EmailDispatchHealthCheck.
// ABOUTME: Verifies Basic Dispatch Mode health reports enabled and intentionally disabled states safely.

using Explore.API.HealthChecks;
using Explore.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class EmailDispatchHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncWhenDispatchEnabledReturnsHealthyWithSafeData()
    {
        var options = Options.Create(new EmailDispatchProcessorSettings
        {
            Enabled = true,
            PollingIntervalSeconds = 7,
            BatchSize = 12,
            MaxAttemptCount = 4,
            ConsumerId = "test-consumer"
        });
        var healthCheck = new EmailDispatchHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("enabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("pollingIntervalSeconds").WhoseValue.Should().Be(7);
        result.Data.Should().ContainKey("batchSize").WhoseValue.Should().Be(12);
        result.Data.Should().ContainKey("maxAttemptCount").WhoseValue.Should().Be(4);
        result.Data.Should().ContainKey("consumerId").WhoseValue.Should().Be("test-consumer");
        result.Data.Keys.Should().NotContain(key => key.Contains("body", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("recipient", StringComparison.OrdinalIgnoreCase));
        result.Data.Keys.Should().NotContain(key => key.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CheckHealthAsyncWhenDispatchDisabledReturnsDegraded()
    {
        var options = Options.Create(new EmailDispatchProcessorSettings
        {
            Enabled = false,
            ConsumerId = "disabled-consumer"
        });
        var healthCheck = new EmailDispatchHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("intentionally disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("consumerId").WhoseValue.Should().Be("disabled-consumer");
    }
}
