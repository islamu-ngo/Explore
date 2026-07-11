// ABOUTME: Unit tests for InfisicalSecretProvider.
// Tests configuration validation, key mapping, and error handling.
// Note: Does not test actual Infisical SDK calls (requires integration tests).

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Explore.Secrets.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Providers;

public class InfisicalSecretProviderTests
{
    private readonly ILogger<InfisicalSecretProvider> _logger;

    public InfisicalSecretProviderTests()
    {
        _logger = Substitute.For<ILogger<InfisicalSecretProvider>>();
    }

    private InfisicalSecretProvider CreateProvider(InfisicalOptions? infisicalOptions = null)
    {
        var options = new SecretProviderOptions
        {
            Provider = SecretProviderType.Infisical,
            Infisical = infisicalOptions ?? new InfisicalOptions
            {
                Url = "https://infisical.example.com",
                ProjectId = "test-project-id",
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret",
                Environment = "dev"
            }
        };

        return new InfisicalSecretProvider(_logger, Options.Create(options));
    }

    [Test]
    public void ProviderType_ShouldReturnInfisical()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        provider.ProviderType.Should().Be(SecretProviderType.Infisical);
    }

    [Test]
    public void SupportsRefresh_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        provider.SupportsRefresh.Should().BeTrue();
    }

    [Test]
    public async Task InitializeAsync_WhenProjectIdMissing_ShouldThrowSecretProviderException()
    {
        // Arrange
        var provider = CreateProvider(new InfisicalOptions
        {
            Url = "https://infisical.example.com",
            ProjectId = null,
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        });

        // Act
        var act = () => provider.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<SecretProviderException>()
            .Where(e => e.ProviderType == SecretProviderType.Infisical)
            .Where(e => e.Operation == "Initialize")
            .Where(e => !e.IsTransient);
    }

    [Test]
    public async Task InitializeAsync_WhenClientIdMissing_ShouldThrowSecretProviderException()
    {
        // Arrange
        var provider = CreateProvider(new InfisicalOptions
        {
            Url = "https://infisical.example.com",
            ProjectId = "test-project",
            ClientId = null,
            ClientSecret = "test-client-secret"
        });

        // Act
        var act = () => provider.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<SecretProviderException>()
            .Where(e => e.ProviderType == SecretProviderType.Infisical)
            .Where(e => e.Operation == "Initialize")
            .Where(e => !e.IsTransient);
    }

    [Test]
    public async Task InitializeAsync_WhenClientSecretMissing_ShouldThrowSecretProviderException()
    {
        // Arrange
        var provider = CreateProvider(new InfisicalOptions
        {
            Url = "https://infisical.example.com",
            ProjectId = "test-project",
            ClientId = "test-client-id",
            ClientSecret = null
        });

        // Act
        var act = () => provider.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<SecretProviderException>()
            .Where(e => e.ProviderType == SecretProviderType.Infisical)
            .Where(e => e.Operation == "Initialize")
            .Where(e => !e.IsTransient);
    }

    [Test]
    public async Task GetSecretAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.GetSecretAsync("SomeKey");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Test]
    public async Task GetSecretsByPathAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.GetSecretsByPathAsync("/api");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Test]
    public async Task RefreshAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.RefreshAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Test]
    public async Task GetHealthAsync_WhenNotInitialized_ShouldReturnUnhealthy()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var health = await provider.GetHealthAsync();

        // Assert
        health.IsHealthy.Should().BeFalse();
        health.ProviderType.Should().Be(SecretProviderType.Infisical);
        health.LastSuccessfulRefresh.Should().BeNull();
        health.ConsecutiveFailures.Should().Be(0);
    }

    [Test]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = async () => await provider.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}
