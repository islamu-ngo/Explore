// ABOUTME: Unit-style tests for the API EmailDispatchHealthCheck.
// ABOUTME: Verifies Basic Dispatch Mode health reports enabled and intentionally disabled states safely.

using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Configuration;
using Explore.API.HealthChecks;
using Explore.Application.Contracts.Persistence;
using Explore.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

[Category(TestCategories.Email)]
public sealed class EmailDispatchHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsyncWhenDispatchEnabledReturnsHealthyWithSafeData()
    {
        var settings = new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.TickerQ,
            PollingIntervalSeconds = 7,
            BatchSize = 12,
            MaxAttemptCount = 4,
            ProcessingLeaseTimeoutSeconds = 30,
            HealthDueDispatchWarningThreshold = 10,
            HealthStaleProcessingWarningThreshold = 2,
            HealthDeadLetterWarningThreshold = 2,
            ConsumerId = "test-consumer"
        };
        var schedulerOptions = new TickerQSchedulerOptions
        {
            Enabled = true,
            DashboardEnabled = false
        };
        var setup = CreateHealthCheck(
            settings,
            schedulerOptions,
            dueDispatchCount: 1,
            retryScheduledCount: 1);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("TickerQ");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.TickerQ));
        result.Data.Should().ContainKey("pollingIntervalSeconds").WhoseValue.Should().Be(7);
        result.Data.Should().ContainKey("batchSize").WhoseValue.Should().Be(12);
        result.Data.Should().ContainKey("maxAttemptCount").WhoseValue.Should().Be(4);
        result.Data.Should().ContainKey("processingLeaseTimeoutSeconds").WhoseValue.Should().Be(30);
        result.Data.Should().ContainKey("dueDispatchWarningThreshold").WhoseValue.Should().Be(10);
        result.Data.Should().ContainKey("staleProcessingWarningThreshold").WhoseValue.Should().Be(2);
        result.Data.Should().ContainKey("deadLetterWarningThreshold").WhoseValue.Should().Be(2);
        result.Data.Should().ContainKey("dueDispatchCount").WhoseValue.Should().Be(1);
        result.Data.Should().ContainKey("retryScheduledCount").WhoseValue.Should().Be(1);
        result.Data.Should().ContainKey("staleProcessingCount").WhoseValue.Should().Be(0);
        result.Data.Should().ContainKey("deadLetteredCount").WhoseValue.Should().Be(0);
        result.Data.Should().ContainKey("processingStartedBefore").WhoseValue.Should().BeOfType<DateTime>();
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
        var settings = new EmailDispatchProcessorSettings
        {
            Enabled = false,
            ConsumerId = "disabled-consumer"
        };
        var setup = CreateHealthCheck(settings);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("intentionally disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(false);
        result.Data.Should().ContainKey("consumerId").WhoseValue.Should().Be("disabled-consumer");
        await setup.Repository.DidNotReceiveWithAnyArgs().CountDueDispatchAsync(default, default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountRetryScheduledAsync(default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountStaleProcessingAsync(default, default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountDeadLetteredAsync(default);
    }

    [Test]
    public async Task CheckHealthAsyncWhenSchedulerModeDisabledReturnsDegraded()
    {
        var settings = new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.Disabled,
            ConsumerId = "disabled-mode"
        };
        var schedulerOptions = new TickerQSchedulerOptions
        {
            Enabled = true
        };
        var setup = CreateHealthCheck(settings, schedulerOptions);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Disabled");
        result.Data.Should().ContainKey("enabled").WhoseValue.Should().Be(true);
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.Disabled));
        await setup.Repository.DidNotReceiveWithAnyArgs().CountDueDispatchAsync(default, default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountRetryScheduledAsync(default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountStaleProcessingAsync(default, default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountDeadLetteredAsync(default);
    }

    [Test]
    public async Task CheckHealthAsyncWhenTickerQModeHasDisabledSchedulerReturnsUnhealthy()
    {
        var settings = new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.TickerQ,
            ConsumerId = "tickerq-disabled"
        };
        var schedulerOptions = new TickerQSchedulerOptions
        {
            Enabled = false
        };
        var setup = CreateHealthCheck(settings, schedulerOptions);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("TickerQ");
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.TickerQ));
        result.Data.Should().ContainKey("tickerQEnabled").WhoseValue.Should().Be(false);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountDueDispatchAsync(default, default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountRetryScheduledAsync(default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountStaleProcessingAsync(default, default);
        await setup.Repository.DidNotReceiveWithAnyArgs().CountDeadLetteredAsync(default);
    }

    [Test]
    public async Task CheckHealthAsyncWhenHostedServiceModeReturnsHealthyWithoutScheduler()
    {
        var settings = new EmailDispatchProcessorSettings
        {
            Enabled = true,
            Mode = EmailDispatchProcessorMode.HostedService,
            ConsumerId = "hosted-service"
        };
        var schedulerOptions = new TickerQSchedulerOptions
        {
            Enabled = false
        };
        var setup = CreateHealthCheck(settings, schedulerOptions);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("hosted service");
        result.Data.Should().ContainKey("mode").WhoseValue.Should().Be(nameof(EmailDispatchProcessorMode.HostedService));
        result.Data.Should().ContainKey("tickerQEnabled").WhoseValue.Should().Be(false);
    }

    [Test]
    public async Task CheckHealthAsyncWhenStaleProcessingAtThresholdReturnsDegraded()
    {
        var setup = CreateHealthCheck(
            new EmailDispatchProcessorSettings
            {
                Enabled = true,
                Mode = EmailDispatchProcessorMode.TickerQ,
                HealthStaleProcessingWarningThreshold = 1,
                ConsumerId = "stale-processing"
            },
            new TickerQSchedulerOptions { Enabled = true },
            staleProcessingCount: 1);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("stale processing");
        result.Data.Should().ContainKey("staleProcessingCount").WhoseValue.Should().Be(1);
    }

    [Test]
    public async Task CheckHealthAsyncWhenDeadLetteredAtThresholdReturnsDegraded()
    {
        var setup = CreateHealthCheck(
            new EmailDispatchProcessorSettings
            {
                Enabled = true,
                Mode = EmailDispatchProcessorMode.TickerQ,
                HealthDeadLetterWarningThreshold = 1,
                ConsumerId = "dead-letter"
            },
            new TickerQSchedulerOptions { Enabled = true },
            deadLetteredCount: 1);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("dead-lettered");
        result.Data.Should().ContainKey("deadLetteredCount").WhoseValue.Should().Be(1);
    }

    [Test]
    public async Task CheckHealthAsyncWhenDueRetryBacklogAtThresholdReturnsDegraded()
    {
        var setup = CreateHealthCheck(
            new EmailDispatchProcessorSettings
            {
                Enabled = true,
                Mode = EmailDispatchProcessorMode.TickerQ,
                HealthDueDispatchWarningThreshold = 2,
                ConsumerId = "retry-backlog"
            },
            new TickerQSchedulerOptions { Enabled = true },
            dueDispatchCount: 2,
            retryScheduledCount: 1);
        using var services = setup.Services;

        var result = await setup.HealthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("due backlog");
        result.Data.Should().ContainKey("dueDispatchCount").WhoseValue.Should().Be(2);
        result.Data.Should().ContainKey("retryScheduledCount").WhoseValue.Should().Be(1);
    }

    private static (
        EmailDispatchHealthCheck HealthCheck,
        ServiceProvider Services,
        IEmailDispatchOutboxRepository Repository) CreateHealthCheck(
            EmailDispatchProcessorSettings settings,
            TickerQSchedulerOptions? schedulerOptions = null,
            int dueDispatchCount = 0,
            int retryScheduledCount = 0,
            int staleProcessingCount = 0,
            int deadLetteredCount = 0)
    {
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        repository.CountDueDispatchAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dueDispatchCount));
        repository.CountRetryScheduledAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(retryScheduledCount));
        repository.CountStaleProcessingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(staleProcessingCount));
        repository.CountDeadLetteredAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(deadLetteredCount));

        var services = new ServiceCollection()
            .AddSingleton(repository)
            .BuildServiceProvider();
        var healthCheck = new EmailDispatchHealthCheck(
            Options.Create(settings),
            Options.Create(schedulerOptions ?? new TickerQSchedulerOptions()),
            services.GetRequiredService<IServiceScopeFactory>());

        return (healthCheck, services, repository);
    }
}
