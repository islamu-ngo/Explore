// ABOUTME: Unit tests for SetupSecretProvider covering secret source, validation, setup mode, and lock behavior.
// ABOUTME: Verifies bootstrap-state gating and internal logging-secret access through InternalsVisibleTo.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public class SetupSecretProviderTests
{
    [Test]
    public async Task Constructor_EnvSecretPresent_UsesEnvironmentValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SETUP_SECRET"] = "my-test-secret" })
            .Build();
        var scopeFactory = CreateScopeFactory(null);
        var provider = new SetupSecretProvider(config, scopeFactory);

        await Assert.That(provider.IsFromEnvironmentVariable).IsEqualTo(true);
        await Assert.That(provider.ValidateSecret("my-test-secret")).IsEqualTo(true);
    }

    [Test]
    public async Task Constructor_EnvSecretMissing_AutoGenerates32CharSecret()
    {
        var provider = CreateProvider();

        await Assert.That(provider.IsSetupSecretRequired).IsEqualTo(true);
        await Assert.That(provider.IsFromEnvironmentVariable).IsEqualTo(false);
        await Assert.That(provider.GetSecretForLogging().Length).IsEqualTo(32);
    }

    [Test]
    public async Task Constructor_SetupSecretRequiredOmitted_DefaultsToRequired()
    {
        var provider = CreateProvider();

        await Assert.That(provider.IsSetupSecretRequired).IsEqualTo(true);
        await Assert.That(provider.ValidateSecret(provider.GetSecretForLogging())).IsEqualTo(true);
    }

    [Test]
    public async Task Constructor_SetupSecretRequiredFalseWithoutTrustedProvisioning_FailsClosedAsRequired()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SETUP_SECRET_REQUIRED"] = "false" })
            .Build();
        var provider = new SetupSecretProvider(configuration, CreateScopeFactory(null));

        await Assert.That(provider.IsSetupSecretRequired).IsEqualTo(true);
        await Assert.That(provider.IsTrustedManagedProvisioningConfigured).IsEqualTo(false);
        await Assert.That(provider.GetSecretForLogging()).IsNotNullOrEmpty();
        await Assert.That(provider.ValidateSecret(provider.GetSecretForLogging())).IsEqualTo(true);
    }

    [Test]
    public async Task Constructor_SetupSecretRequiredFalseWithTrustedManagedProvisioning_DisablesInteractiveSetupSecret()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SETUP_SECRET_REQUIRED"] = "false",
                ["PROVISIONING_TRUSTED"] = "true",
                ["PROVISIONING_MODE"] = "managed-provider",
                ["MANAGED_CLIENT_EXTERNAL_PROVIDER"] = "erp",
                ["PHYSICAL_TENANCY_MODE"] = "shared-database"
            })
            .Build();
        var provider = new SetupSecretProvider(configuration, CreateScopeFactory(null));

        await Assert.That(provider.IsSetupSecretRequired).IsEqualTo(false);
        await Assert.That(provider.IsTrustedManagedProvisioningConfigured).IsEqualTo(true);
        await Assert.That(provider.GetSecretForLogging()).IsNull();
        await Assert.That(provider.ValidateSecret(null)).IsEqualTo(false);
        await Assert.That(provider.ValidateSecret("anything")).IsEqualTo(false);
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
        await provider.InitializeAsync();

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(false);
    }

    [Test]
    public async Task IsSetupModeActive_WhenBootstrapNotCompleteAndNotLocked_ReturnsTrue()
    {
        var provider = CreateProvider(new InstanceBootstrapState { IsCompleted = false });
        await provider.InitializeAsync();

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(true);
    }

    [Test]
    public async Task IsSetupModeActive_BeforeInitialize_ReturnsFalse()
    {
        var provider = CreateProvider(new InstanceBootstrapState { IsCompleted = false });

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(false);
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
        var provider = CreateProvider(new InstanceBootstrapState { IsCompleted = false });
        await provider.InitializeAsync();
        var secret = provider.GetSecretForLogging();

        await Assert.That(provider.IsSetupModeActive).IsEqualTo(true);

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
        var scopeFactory = CreateScopeFactory(state);
        return new SetupSecretProvider(configuration, scopeFactory);
    }

    private static IServiceScopeFactory CreateScopeFactory(InstanceBootstrapState? bootstrapState)
    {
        var repository = Substitute.For<IInstanceBootstrapStateRepository>();
        repository.GetCurrent().Returns(Task.FromResult(bootstrapState));

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(IInstanceBootstrapStateRepository)).Returns(repository);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return scopeFactory;
    }
}
