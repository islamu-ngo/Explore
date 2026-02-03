// ABOUTME: Unit tests for EnvironmentSecretProvider.
// Tests key mapping, secret retrieval, and health status.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Providers;

public class EnvironmentSecretProviderTests
{
    private readonly ILogger<EnvironmentSecretProvider> _logger;
    private readonly EnvironmentSecretProvider _provider;

    public EnvironmentSecretProviderTests()
    {
        _logger = Substitute.For<ILogger<EnvironmentSecretProvider>>();
        _provider = new EnvironmentSecretProvider(_logger);
    }

    [Test]
    public void ProviderType_ShouldReturnNone()
    {
        _provider.ProviderType.Should().Be(SecretProviderType.None);
    }

    [Test]
    public void SupportsRefresh_ShouldReturnFalse()
    {
        _provider.SupportsRefresh.Should().BeFalse();
    }

    [Test]
    public async Task InitializeAsync_ShouldComplete()
    {
        await _provider.InitializeAsync();
        // Should not throw
    }

    [Test]
    public async Task GetSecretAsync_WhenNotInitialized_ShouldThrow()
    {
        var act = async () => await _provider.GetSecretAsync("Test:Key");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Test]
    public async Task GetSecretAsync_WhenEnvVarExists_ShouldReturnValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST__SECRET", "secret-value");
        await _provider.InitializeAsync();

        try
        {
            // Act
            var result = await _provider.GetSecretAsync("Test:Secret");

            // Assert
            result.Should().Be("secret-value");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST__SECRET", null);
        }
    }

    [Test]
    public async Task GetSecretAsync_WhenEnvVarNotExists_ShouldReturnNull()
    {
        // Arrange
        await _provider.InitializeAsync();

        // Act
        var result = await _provider.GetSecretAsync("NonExistent:Key");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetSecretWithMetadataAsync_WhenExists_ShouldReturnSecretValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("METADATA__TEST", "test-value");
        await _provider.InitializeAsync();

        try
        {
            // Act
            var result = await _provider.GetSecretWithMetadataAsync("Metadata:Test");

            // Assert
            result.Should().NotBeNull();
            result!.Value.Should().Be("test-value");
            result.Version.Should().BeNull(); // Env vars don't have versions
        }
        finally
        {
            Environment.SetEnvironmentVariable("METADATA__TEST", null);
        }
    }

    [Test]
    public async Task GetSecretWithMetadataAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        await _provider.InitializeAsync();

        // Act
        var result = await _provider.GetSecretWithMetadataAsync("NonExistent:Key");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetSecretsByPathAsync_ShouldReturnMatchingSecrets()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PREFIX__KEY1", "value1");
        Environment.SetEnvironmentVariable("PREFIX__KEY2", "value2");
        Environment.SetEnvironmentVariable("OTHER__KEY", "other");
        await _provider.InitializeAsync();

        try
        {
            // Act
            var results = await _provider.GetSecretsByPathAsync("Prefix");

            // Assert
            results.Should().ContainKey("Prefix:Key1");
            results.Should().ContainKey("Prefix:Key2");
            results.Should().NotContainKey("Other:Key");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PREFIX__KEY1", null);
            Environment.SetEnvironmentVariable("PREFIX__KEY2", null);
            Environment.SetEnvironmentVariable("OTHER__KEY", null);
        }
    }

    [Test]
    public async Task RefreshAsync_ShouldNotThrow()
    {
        // Arrange
        await _provider.InitializeAsync();

        // Act & Assert - Refresh should complete without error
        await _provider.RefreshAsync();
    }

    [Test]
    public async Task GetHealthAsync_WhenInitialized_ShouldReturnHealthy()
    {
        // Arrange
        await _provider.InitializeAsync();

        // Act
        var health = await _provider.GetHealthAsync();

        // Assert
        health.IsHealthy.Should().BeTrue();
        health.ProviderType.Should().Be(SecretProviderType.None);
        health.ConsecutiveFailures.Should().Be(0);
    }

    [Test]
    public async Task GetHealthAsync_WhenNotInitialized_ShouldReturnUnhealthy()
    {
        // Act
        var health = await _provider.GetHealthAsync();

        // Assert
        health.IsHealthy.Should().BeFalse();
    }
}
