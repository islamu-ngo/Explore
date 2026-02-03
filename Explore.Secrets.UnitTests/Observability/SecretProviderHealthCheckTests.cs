// ABOUTME: Unit tests for SecretProviderHealthCheck.
// Tests health check responses based on provider status.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Observability;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Observability;

public class SecretProviderHealthCheckTests : IDisposable
{
    private readonly ISecretProvider _provider;
    private readonly ILogger<SecretProviderHealthCheck> _logger;
    private readonly SecretRefreshMetrics _metrics;
    private readonly SecretProviderHealthCheck _healthCheck;

    public SecretProviderHealthCheckTests()
    {
        _provider = Substitute.For<ISecretProvider>();
        _logger = Substitute.For<ILogger<SecretProviderHealthCheck>>();
        _metrics = new SecretRefreshMetrics();
        _healthCheck = new SecretProviderHealthCheck(_provider, _logger, _metrics);
    }

    public void Dispose()
    {
        _metrics.Dispose();
    }

    [Test]
    public async Task CheckHealthAsync_WhenProviderHealthy_ShouldReturnHealthy()
    {
        // Arrange
        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ProviderHealthInfo(
                ProviderType: SecretProviderType.Infisical,
                IsHealthy: true,
                ConsecutiveFailures: 0,
                LastSuccessfulRefresh: DateTimeOffset.UtcNow,
                ErrorMessage: null));

        _provider.SupportsRefresh.Returns(true);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Infisical");
        result.Description.Should().Contain("healthy");
    }

    [Test]
    public async Task CheckHealthAsync_WhenProviderDegraded_ShouldReturnDegraded()
    {
        // Arrange
        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ProviderHealthInfo(
                ProviderType: SecretProviderType.Vault,
                IsHealthy: false,
                ConsecutiveFailures: 2, // Less than 3 = degraded
                LastSuccessfulRefresh: DateTimeOffset.UtcNow.AddMinutes(-5),
                ErrorMessage: "Temporary connection issue"));

        _provider.SupportsRefresh.Returns(true);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("degraded");
    }

    [Test]
    public async Task CheckHealthAsync_WhenProviderUnhealthy_ShouldReturnUnhealthy()
    {
        // Arrange
        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ProviderHealthInfo(
                ProviderType: SecretProviderType.AzureKeyVault,
                IsHealthy: false,
                ConsecutiveFailures: 5, // 3+ = unhealthy
                LastSuccessfulRefresh: DateTimeOffset.UtcNow.AddMinutes(-30),
                ErrorMessage: "Authentication failed"));

        _provider.SupportsRefresh.Returns(true);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("5 consecutive failures");
    }

    [Test]
    public async Task CheckHealthAsync_WhenProviderThrows_ShouldReturnUnhealthy()
    {
        // Arrange
        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns<Task<ProviderHealthInfo>>(x => throw new InvalidOperationException("Provider crashed"));

        _provider.ProviderType.Returns(SecretProviderType.AwsSecretsManager);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Description.Should().Contain("exception");
    }

    [Test]
    public async Task CheckHealthAsync_ShouldIncludeProviderTypeInData()
    {
        // Arrange
        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ProviderHealthInfo(
                ProviderType: SecretProviderType.Infisical,
                IsHealthy: true,
                ConsecutiveFailures: 0,
                LastSuccessfulRefresh: DateTimeOffset.UtcNow,
                ErrorMessage: null));

        _provider.SupportsRefresh.Returns(true);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("provider");
        result.Data["provider"].Should().Be("Infisical");
    }

    [Test]
    public async Task CheckHealthAsync_ShouldIncludeSupportsRefreshInData()
    {
        // Arrange
        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ProviderHealthInfo(
                ProviderType: SecretProviderType.None,
                IsHealthy: true,
                ConsecutiveFailures: 0,
                LastSuccessfulRefresh: null,
                ErrorMessage: null));

        _provider.SupportsRefresh.Returns(false);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("supportsRefresh");
        result.Data["supportsRefresh"].Should().Be(false);
    }

    [Test]
    public async Task CheckHealthAsync_ShouldIncludeLastRefreshTimestamp()
    {
        // Arrange
        var lastRefresh = DateTimeOffset.UtcNow.AddMinutes(-10);
        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ProviderHealthInfo(
                ProviderType: SecretProviderType.Vault,
                IsHealthy: true,
                ConsecutiveFailures: 0,
                LastSuccessfulRefresh: lastRefresh,
                ErrorMessage: null));

        _provider.SupportsRefresh.Returns(true);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("lastSuccessfulRefresh");
        result.Data.Should().ContainKey("secondsSinceLastRefresh");
    }

    [Test]
    public void Name_ShouldBeSecretProvider()
    {
        SecretProviderHealthCheck.Name.Should().Be("secret_provider");
    }

    [Test]
    public void Tag_ShouldBeSecrets()
    {
        SecretProviderHealthCheck.Tag.Should().Be("secrets");
    }

    [Test]
    public async Task CheckHealthAsync_WithoutMetrics_ShouldStillWork()
    {
        // Arrange - Create health check without metrics
        var healthCheckWithoutMetrics = new SecretProviderHealthCheck(_provider, _logger, metrics: null);

        _provider.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ProviderHealthInfo(
                ProviderType: SecretProviderType.Infisical,
                IsHealthy: true,
                ConsecutiveFailures: 0,
                LastSuccessfulRefresh: DateTimeOffset.UtcNow,
                ErrorMessage: null));

        _provider.SupportsRefresh.Returns(true);

        // Act
        var result = await healthCheckWithoutMetrics.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
