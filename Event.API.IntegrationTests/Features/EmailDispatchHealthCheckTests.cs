// ABOUTME: Unit-style tests for the API EmailDispatchHealthCheck.
// ABOUTME: Verifies Basic Dispatch Mode health reports enabled and intentionally disabled states safely.

using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Configuration;
using Explore.API.HealthChecks;
using Explore.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

[Category(TestCategories.Email)]
public sealed class EmailDispatchHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncWhenDispatchEnabledReturnsHealthyWithSafeData()
    {
        var options = Options.Create(new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.TickerQ,
            PollingIntervalSeconds = 7,
            BatchSize = 12,
            MaxAttemptCount = 4,
            ConsumerId = "test-consumer"
        });
        var schedulerOptions = Options.Create(new TickerQSchedulerOptions
        {
            Enabled = true,
            DashboardEnabled = false
        });
        var healthCheck = new EmailDispatchHealthCheck(options, schedulerOptions);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("TickerQ");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.TickerQ));
        result.Data.Should().ContainKey("pollingIntervalSeconds").WhoseValue.Should().Be(7);
        result.Data.Should().ContainKey("batchSize").WhoseValue.Should().Be(12);
        result.Data.Should().ContainKey("maxAttemptCount").WhoseValue.Should().Be(4);
        result.Data.Should().ContainKey("consumerId").WhoseValue.Should().Be("test-consumer");
        result.Data.Should().ContainKey("tickerQEnabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("tickerQDashboardEnabled").WhoseValue.Should().Be(false);
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
        var schedulerOptions = Options.Create(new TickerQSchedulerOptions());
        var healthCheck = new EmailDispatchHealthCheck(options, schedulerOptions);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("intentionally disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("consumerId").WhoseValue.Should().Be("disabled-consumer");
    }

    [Test]
    public async Task CheckHealthAsyncWhenSchedulerModeDisabledReturnsDegraded()
    {
        var options = Options.Create(new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.Disabled,
            ConsumerId = "disabled-mode"
        });
        var schedulerOptions = Options.Create(new TickerQSchedulerOptions
        {
            Enabled = true
        });
        var healthCheck = new EmailDispatchHealthCheck(options, schedulerOptions);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.Disabled));
    }

    [Test]
    public async Task CheckHealthAsyncWhenTickerQModeHasDisabledSchedulerReturnsUnhealthy()
    {
        var options = Options.Create(new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.TickerQ,
            ConsumerId = "tickerq-disabled"
        });
        var schedulerOptions = Options.Create(new TickerQSchedulerOptions
        {
            Enabled = false
        });
        var healthCheck = new EmailDispatchHealthCheck(options, schedulerOptions);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("TickerQ");
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.TickerQ));
        result.Data.Should().ContainKey("tickerQEnabled").WhoseValue.Should().Be(false);
    }

    [Test]
    public async Task CheckHealthAsyncWhenHostedServiceModeReturnsHealthyWithoutScheduler()
    {
        var options = Options.Create(new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.HostedService,
            ConsumerId = "hosted-service"
        });
        var schedulerOptions = Options.Create(new TickerQSchedulerOptions
        {
            Enabled = false
        });
        var healthCheck = new EmailDispatchHealthCheck(options, schedulerOptions);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("hosted service");
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.HostedService));
        result.Data.Should().ContainKey("tickerQEnabled").WhoseValue.Should().Be(false);
    }
}
