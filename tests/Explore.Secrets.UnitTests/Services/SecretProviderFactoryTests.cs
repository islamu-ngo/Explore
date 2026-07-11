// ABOUTME: Unit tests for SecretProviderFactory.
// Tests factory creation based on provider type configuration.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Explore.Secrets.Providers;
using Explore.Secrets.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Services;

public class SecretProviderFactoryTests
{
    private readonly ILoggerFactory _loggerFactory;

    public SecretProviderFactoryTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>())
            .Returns(Substitute.For<ILogger>());
        _loggerFactory.CreateLogger<EnvironmentSecretProvider>()
            .Returns(Substitute.For<ILogger<EnvironmentSecretProvider>>());
        _loggerFactory.CreateLogger<InfisicalSecretProvider>()
            .Returns(Substitute.For<ILogger<InfisicalSecretProvider>>());
        _loggerFactory.CreateLogger<SecretProviderFactory>()
            .Returns(Substitute.For<ILogger<SecretProviderFactory>>());
    }

    [Test]
    public void Create_WhenProviderTypeIsNone_ShouldReturnEnvironmentProvider()
    {
        // Arrange
        var options = Options.Create(new SecretProviderOptions
        {
            Provider = SecretProviderType.None
        });
        var factory = new SecretProviderFactory(options, _loggerFactory);

        // Act
        var provider = factory.Create();

        // Assert
        provider.Should().BeOfType<EnvironmentSecretProvider>();
        provider.ProviderType.Should().Be(SecretProviderType.None);
    }

    [Test]
    public void Create_WhenProviderTypeIsInfisical_ShouldReturnInfisicalProvider()
    {
        // Arrange
        var options = Options.Create(new SecretProviderOptions
        {
            Provider = SecretProviderType.Infisical
        });
        var factory = new SecretProviderFactory(options, _loggerFactory);

        // Act
        var provider = factory.Create();

        // Assert
        provider.Should().BeOfType<InfisicalSecretProvider>();
        provider.ProviderType.Should().Be(SecretProviderType.Infisical);
        provider.SupportsRefresh.Should().BeTrue();
    }

    [Test]
    public void Create_WhenProviderTypeIsVault_ShouldThrowNotImplemented()
    {
        // Arrange
        var options = Options.Create(new SecretProviderOptions
        {
            Provider = SecretProviderType.Vault
        });
        var factory = new SecretProviderFactory(options, _loggerFactory);

        // Act
        var act = () => factory.Create();

        // Assert
        act.Should().Throw<SecretProviderException>()
            .Which.ProviderType.Should().Be(SecretProviderType.Vault);
    }

    [Test]
    public void Create_WhenProviderTypeIsAzureKeyVault_ShouldThrowNotImplemented()
    {
        // Arrange
        var options = Options.Create(new SecretProviderOptions
        {
            Provider = SecretProviderType.AzureKeyVault
        });
        var factory = new SecretProviderFactory(options, _loggerFactory);

        // Act
        var act = () => factory.Create();

        // Assert
        act.Should().Throw<SecretProviderException>()
            .Which.ProviderType.Should().Be(SecretProviderType.AzureKeyVault);
    }

    [Test]
    public void Create_WhenProviderTypeIsAwsSecretsManager_ShouldThrowNotImplemented()
    {
        // Arrange
        var options = Options.Create(new SecretProviderOptions
        {
            Provider = SecretProviderType.AwsSecretsManager
        });
        var factory = new SecretProviderFactory(options, _loggerFactory);

        // Act
        var act = () => factory.Create();

        // Assert
        act.Should().Throw<SecretProviderException>()
            .Which.ProviderType.Should().Be(SecretProviderType.AwsSecretsManager);
    }
}
