// ABOUTME: Unit tests for SecretRefreshService.
// ABOUTME: Tests refresh scheduling, backoff behavior, and metrics integration.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Explore.Secrets.Observability;
using Explore.Secrets.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Services;

public class SecretRefreshServiceTests : IDisposable
{
    private readonly ISecretProvider _mockProvider;
    private readonly IConfiguration _mockConfiguration;
    private readonly SecretRefreshMetrics _metrics;
    private readonly ILogger<SecretRefreshService> _logger;

    public SecretRefreshServiceTests()
    {
        _mockProvider = Substitute.For<ISecretProvider>();
        _mockProvider.ProviderType.Returns(SecretProviderType.Infisical);
        _mockProvider.SupportsRefresh.Returns(true);

        _mockConfiguration = Substitute.For<IConfiguration>();
        _metrics = new SecretRefreshMetrics();
        _logger = Substitute.For<ILogger<SecretRefreshService>>();
    }

    private SecretRefreshService CreateService(SecretRefreshOptions? options = null)
    {
        var refreshOptions = options ?? new SecretRefreshOptions
        {
            Enabled = true,
            RefreshInterval = TimeSpan.FromSeconds(1),
            InitialDelay = TimeSpan.FromMilliseconds(10),
            BaseBackoffDelay = TimeSpan.FromMilliseconds(100),
            MaxBackoffDelay = TimeSpan.FromSeconds(1),
            JitterFactor = 0 // Disable jitter for predictable tests
        };

        return new SecretRefreshService(
            _mockProvider,
            _mockConfiguration,
            Options.Create(refreshOptions),
            _metrics,
            _logger);
    }

    [Test]
    public async Task ConsecutiveFailures_Initially_ShouldBeZero()
    {
        // Arrange
        var service = CreateService();

        // Assert
        await Assert.That(service.ConsecutiveFailures).IsEqualTo(0);
    }

    [Test]
    public async Task LastSuccessfulRefresh_Initially_ShouldBeNull()
    {
        // Arrange
        var service = CreateService();

        // Assert
        await Assert.That(service.LastSuccessfulRefresh).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_WhenDisabled_ShouldReturnImmediately()
    {
        // Arrange
        var options = new SecretRefreshOptions { Enabled = false };
        var service = CreateService(options);
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(50); // Give it time to potentially do work
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        await _mockProvider.DidNotReceive().RefreshAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenProviderDoesNotSupportRefresh_ShouldReturnImmediately()
    {
        // Arrange
        _mockProvider.SupportsRefresh.Returns(false);
        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(50);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        await _mockProvider.DidNotReceive().RefreshAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelled_ShouldStopGracefully()
    {
        // Arrange
        var options = new SecretRefreshOptions
        {
            Enabled = true,
            InitialDelay = TimeSpan.FromSeconds(10) // Long delay so we cancel during wait
        };
        var service = CreateService(options);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await cts.CancelAsync();

        // Should not throw
        await service.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task ExecuteAsync_WhenRefreshSucceeds_ShouldUpdateLastSuccessfulRefresh()
    {
        // Arrange
        _mockProvider.RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var options = new SecretRefreshOptions
        {
            Enabled = true,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            RefreshInterval = TimeSpan.FromSeconds(10),
            JitterFactor = 0
        };
        var service = CreateService(options);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100); // Wait for initial refresh
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        await _mockProvider.Received().RefreshAsync(Arg.Any<CancellationToken>());
        await Assert.That(service.ConsecutiveFailures).IsEqualTo(0);
        await Assert.That(service.LastSuccessfulRefresh).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_WhenRefreshFails_ShouldIncrementConsecutiveFailures()
    {
        // Arrange
        _mockProvider.RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(SecretProviderException.Transient(
                "Test error",
                SecretProviderType.Infisical,
                "Refresh")));

        var options = new SecretRefreshOptions
        {
            Enabled = true,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            RefreshInterval = TimeSpan.FromSeconds(10),
            JitterFactor = 0
        };
        var service = CreateService(options);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100); // Wait for failed refresh
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        await _mockProvider.Received().RefreshAsync(Arg.Any<CancellationToken>());
        await Assert.That(service.ConsecutiveFailures).IsGreaterThan(0);
        await Assert.That(service.LastSuccessfulRefresh).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_WhenRefreshSucceedsAfterFailure_ShouldResetConsecutiveFailures()
    {
        // Arrange
        var callCount = 0;
        _mockProvider.RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.FromException(SecretProviderException.Transient(
                        "Test error",
                        SecretProviderType.Infisical,
                        "Refresh"));
                }
                return Task.CompletedTask;
            });

        var options = new SecretRefreshOptions
        {
            Enabled = true,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            RefreshInterval = TimeSpan.FromMilliseconds(50),
            BaseBackoffDelay = TimeSpan.FromMilliseconds(10),
            MaxBackoffDelay = TimeSpan.FromMilliseconds(100),
            JitterFactor = 0
        };
        var service = CreateService(options);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200); // Wait for multiple refresh cycles
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        await Assert.That(callCount).IsGreaterThan(1);
        await Assert.That(service.ConsecutiveFailures).IsEqualTo(0);
        await Assert.That(service.LastSuccessfulRefresh).IsNotNull();
    }

    public void Dispose()
    {
        _metrics.Dispose();
    }
}
