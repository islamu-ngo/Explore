// ABOUTME: Unit tests for SetupSecretProvider covering secret source, validation, setup mode, and lock behavior.
// ABOUTME: Verifies bootstrap-state gating and internal logging-secret access through InternalsVisibleTo.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure;

public class SetupSecretProviderTests
{
    [Test]
    public async Task Constructor_EnvSecretPresent_UsesEnvironmentValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SETUP_SECRET"] = "my-test-secret" })
            .Build();
        var serviceProvider = CreateServiceProvider(null);
        var provider = new SetupSecretProvider(config, serviceProvider);

        await Assert.That(provider.IsFromEnvironmentVariable).IsEqualTo(true);
        await Assert.That(provider.ValidateSecret("my-test-secret")).IsEqualTo(true);
    }

    [Test]
    public async Task Constructor_EnvSecretMissing_AutoGenerates32CharSecret()
    {
        var provider = CreateProvider();

        await Assert.That(provider.IsFromEnvironmentVariable).IsEqualTo(false);
        await Assert.That(provider.GetSecretForLogging().Length).IsEqualTo(32);
    }

    [Test]
    public async Task ValidateSecret_CorrectSecret_ReturnsTrue()
    {
        var provider = CreateProvider();
        var secret = provider.GetSecretForLogging();

        await Assert.That(provider.ValidateSecret(secret)).IsEqualTo(true);
    }

    [Test]
    public async Task ValidateSecret_WrongSecret_ReturnsFalse()
    {
        var provider = CreateProvider();

        await Assert.That(provider.ValidateSecret("wrong-secret")).IsEqualTo(false);
    }

    [Test]
    public async Task ValidateSecret_NullOrEmpty_ReturnsFalse()
    {
        var provider = CreateProvider();

        await Assert.That(provider.ValidateSecret(null)).IsEqualTo(false);
        await Assert.That(provider.ValidateSecret(string.Empty)).IsEqualTo(false);
    }

    [Test]
    public async Task ValidateSecret_AfterLock_ReturnsFalse()
    {
        var provider = CreateProvider();
        var secret = provider.GetSecretForLogging();

        provider.Lock();

        await Assert.That(provider.ValidateSecret(secret)).IsEqualTo(false);
    }

    [Test]
    public async Task IsSetupModeActive_WhenLocked_ReturnsFalse()
    {
        var provider = CreateProvider();

        provider.Lock();

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(false);
    }

    [Test]
    public async Task IsSetupModeActive_WhenBootstrapComplete_ReturnsFalse()
    {
        var provider = CreateProvider(new InstanceBootstrapState { IsCompleted = true });

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(false);
    }

    [Test]
    public async Task IsSetupModeActive_WhenBootstrapNotCompleteAndNotLocked_ReturnsTrue()
    {
        var provider = CreateProvider(new InstanceBootstrapState { IsCompleted = false });

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(true);
    }

    [Test]
    public async Task IsTimedOut_FreshInstance_ReturnsFalse()
    {
        var provider = CreateProvider();

        await Assert.That(provider.IsTimedOut).IsEqualTo(false);
    }

    [Test]
    public async Task Lock_AppliesSetupAndValidationTransitions()
    {
        var provider = CreateProvider();
        var secret = provider.GetSecretForLogging();

        provider.Lock();

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(false);
        await Assert.That(provider.ValidateSecret(secret)).IsEqualTo(false);
    }

    [Test]
    public async Task GetSecretForLogging_ReturnsSecretValue()
    {
        var provider = CreateProvider();
        var secret = provider.GetSecretForLogging();

        await Assert.That(secret).IsNotNullOrEmpty();
        await Assert.That(provider.ValidateSecret(secret)).IsEqualTo(true);
    }

    private static SetupSecretProvider CreateProvider(InstanceBootstrapState? state = null)
    {
        var configuration = new ConfigurationBuilder().Build();
        var serviceProvider = CreateServiceProvider(state);
        return new SetupSecretProvider(configuration, serviceProvider);
    }

    private static IServiceProvider CreateServiceProvider(InstanceBootstrapState? bootstrapState)
    {
        var repository = Substitute.For<IInstanceBootstrapStateRepository>();
        repository.GetCurrent().Returns(Task.FromResult(bootstrapState));

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(IInstanceBootstrapStateRepository)).Returns(repository);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        return serviceProvider;
    }
}
