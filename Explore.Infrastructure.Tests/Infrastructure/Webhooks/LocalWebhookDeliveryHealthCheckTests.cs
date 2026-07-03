// ABOUTME: Unit tests for the LocalProvider webhook delivery readiness health check.
// ABOUTME: Verifies provider selection, disabled processor state, queue backlog, and stale lease reporting.

using Explore.Application.Contracts.Persistence;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.HealthChecks;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class LocalWebhookDeliveryHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenLocalProviderNotSelected_ReturnsHealthyWithoutQueueQuery()
    {
        var attemptRepository = Substitute.For<IWebhookDeliveryAttemptRepository>();
        var healthCheck = CreateHealthCheck(
            attemptRepository,
            new WebhookOptions { Enabled = true, Provider = WebhookOptions.ProviderSvix },
            new WebhookDeliveryProcessorSettings());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["localProviderSelected"]).IsEqualTo(false);
        await attemptRepository.DidNotReceiveWithAnyArgs().CountDueScheduledAsync(default, default);
        await attemptRepository.DidNotReceiveWithAnyArgs().CountStaleSendingAsync(default, default);
    }

    [Test]
    public async Task CheckHealthAsync_WhenProcessorDisabled_ReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(
            Substitute.For<IWebhookDeliveryAttemptRepository>(),
            new WebhookOptions { Enabled = true, Provider = WebhookOptions.ProviderLocal },
            new WebhookDeliveryProcessorSettings { Enabled = false });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Degraded);
        await Assert.That(result.Data["processorEnabled"]).IsEqualTo(false);
    }

    [Test]
    public async Task CheckHealthAsync_WhenQueueBelowThreshold_ReturnsHealthyWithSafeCounts()
    {
        var attemptRepository = Substitute.For<IWebhookDeliveryAttemptRepository>();
        attemptRepository.CountDueScheduledAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(5);
        attemptRepository.CountStaleSendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);
        var healthCheck = CreateHealthCheck(
            attemptRepository,
            new WebhookOptions { Enabled = true, Provider = WebhookOptions.ProviderLocal },
            new WebhookDeliveryProcessorSettings
            {
                HealthDueAttemptWarningThreshold = 10,
                HealthStaleSendingWarningThreshold = 1
            });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["dueScheduledAttempts"]).IsEqualTo(5);
        await Assert.That(result.Data["staleSendingAttempts"]).IsEqualTo(0);
        await Assert.That(result.Data.Keys).DoesNotContain("endpointUrl");
        await Assert.That(result.Data.Keys).DoesNotContain("payloadJson");
        await Assert.That(result.Data.Keys).DoesNotContain("secretRef");
    }

    [Test]
    public async Task CheckHealthAsync_WhenStaleSendingAtThreshold_ReturnsDegraded()
    {
        var attemptRepository = Substitute.For<IWebhookDeliveryAttemptRepository>();
        attemptRepository.CountDueScheduledAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0);
        attemptRepository.CountStaleSendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(1);
        var healthCheck = CreateHealthCheck(
            attemptRepository,
            new WebhookOptions { Enabled = true, Provider = WebhookOptions.ProviderLocal },
            new WebhookDeliveryProcessorSettings
            {
                HealthDueAttemptWarningThreshold = 10,
                HealthStaleSendingWarningThreshold = 1
            });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Degraded);
        await Assert.That(result.Data["staleSendingAttempts"]).IsEqualTo(1);
    }

    private static LocalWebhookDeliveryHealthCheck CreateHealthCheck(
        IWebhookDeliveryAttemptRepository attemptRepository,
        WebhookOptions webhookOptions,
        WebhookDeliveryProcessorSettings settings)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => attemptRepository);
        var serviceProvider = services.BuildServiceProvider();
        return new LocalWebhookDeliveryHealthCheck(
            Options.Create(settings),
            new StaticOptionsMonitor<WebhookOptions>(webhookOptions),
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
