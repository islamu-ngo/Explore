// ABOUTME: Unit tests for the Svix webhook provider readiness health check.
// ABOUTME: Verifies provider selection and secret-resolution reporting without leaking sensitive values.

using Explore.Application.Contracts.Secrets;
using Explore.Domain.Enums;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class SvixWebhookProviderHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenSvixProviderNotSelected_ReturnsHealthyWithoutResolvingSecrets()
    {
        var secretResolver = Substitute.For<ISecretResolver>();
        var healthCheck = CreateHealthCheck(
            secretResolver,
            new WebhookOptions { Enabled = true, Provider = WebhookOptions.ProviderLocal });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["svixProviderSelected"]).IsEqualTo(false);
        await secretResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, default, default);
    }

    [Test]
    public async Task CheckHealthAsync_WhenSvixAuthTokenCannotResolve_ReturnsUnhealthyWithoutSecretData()
    {
        var secretResolver = Substitute.For<ISecretResolver>();
        secretResolver.ResolveAsync("webhooks.svix.auth_token", null, Arg.Any<CancellationToken>())
            .Returns((ResolvedSecret?)null);
        var healthCheck = CreateHealthCheck(
            secretResolver,
            new WebhookOptions
            {
                Enabled = true,
                Provider = WebhookOptions.ProviderSvix,
                Svix = new WebhookSvixOptions
                {
                    BaseUrl = "http://svix:8071",
                    AuthTokenSecretRef = "webhooks.svix.auth_token",
                    OperationalWebhookSecretRef = null
                }
            });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Data["authTokenResolved"]).IsEqualTo(false);
        await Assert.That(result.Data.Keys).DoesNotContain("baseUrl");
        await Assert.That(result.Data.Keys).DoesNotContain("authToken");
        await Assert.That(result.Data.Keys).DoesNotContain("secretRef");
    }

    [Test]
    public async Task CheckHealthAsync_WhenSvixSecretsResolve_ReturnsHealthy()
    {
        var secretResolver = Substitute.For<ISecretResolver>();
        secretResolver.ResolveAsync("webhooks.svix.auth_token", null, Arg.Any<CancellationToken>())
            .Returns(Resolved("webhooks.svix.auth_token", "jwt-token"));
        secretResolver.ResolveAsync("webhooks.svix.operational_webhook_secret", null, Arg.Any<CancellationToken>())
            .Returns(Resolved("webhooks.svix.operational_webhook_secret", "whsec_secret"));
        var healthCheck = CreateHealthCheck(
            secretResolver,
            new WebhookOptions
            {
                Enabled = true,
                Provider = WebhookOptions.ProviderComposite,
                Svix = new WebhookSvixOptions
                {
                    BaseUrl = "http://svix:8071",
                    AuthTokenSecretRef = "webhooks.svix.auth_token",
                    OperationalWebhookSecretRef = "webhooks.svix.operational_webhook_secret"
                }
            });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["svixProviderSelected"]).IsEqualTo(true);
        await Assert.That(result.Data["authTokenResolved"]).IsEqualTo(true);
        await Assert.That(result.Data["operationalWebhookSecretResolved"]).IsEqualTo(true);
    }

    [Test]
    public async Task CheckHealthAsync_WhenOperationalWebhookSecretCannotResolve_ReturnsHealthyWithSafeFlag()
    {
        var secretResolver = Substitute.For<ISecretResolver>();
        secretResolver.ResolveAsync("webhooks.svix.auth_token", null, Arg.Any<CancellationToken>())
            .Returns(Resolved("webhooks.svix.auth_token", "jwt-token"));
        secretResolver.ResolveAsync("webhooks.svix.operational_webhook_secret", null, Arg.Any<CancellationToken>())
            .Returns((ResolvedSecret?)null);
        var healthCheck = CreateHealthCheck(
            secretResolver,
            new WebhookOptions
            {
                Enabled = true,
                Provider = WebhookOptions.ProviderSvix,
                Svix = new WebhookSvixOptions
                {
                    AuthTokenSecretRef = "webhooks.svix.auth_token",
                    OperationalWebhookSecretRef = "webhooks.svix.operational_webhook_secret"
                }
            });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["operationalWebhookSecretResolved"]).IsEqualTo(false);
    }

    private static SvixWebhookProviderHealthCheck CreateHealthCheck(
        ISecretResolver secretResolver,
        WebhookOptions options)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => secretResolver);
        var serviceProvider = services.BuildServiceProvider();

        return new SvixWebhookProviderHealthCheck(
            new StaticOptionsMonitor<WebhookOptions>(options),
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    private static ResolvedSecret Resolved(string settingKey, string value) =>
        new(
            settingKey,
            value,
            SecretSourceType.EnvironmentVariable,
            SecretScope.Instance,
            ScopeId: null,
            DateTimeOffset.UtcNow);

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
