// ABOUTME: Unit tests for authentication-provider configuration resolution.
// ABOUTME: Verifies deployment Keycloak fallback, persisted precedence, configured status, and secret redaction.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class AuthProviderConfigurationServiceTests
{
    [Test]
    public async Task ReadConfigurationAsync_WithDeploymentKeycloak_UsesSanitizedDeploymentFallback()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://id.example.test/realms/events",
            ["Keycloak:ClientId"] = "event-blazor",
            ["Keycloak:ClientSecret"] = "must-not-leave-the-server"
        });

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.KeycloakEnabled).IsTrue();
        await Assert.That(result.KeycloakDetectedFromEnvironment).IsTrue();
        await Assert.That(result.KeycloakAuthority).IsEqualTo("https://id.example.test/realms/events");
        await Assert.That(result.KeycloakClientId).IsEqualTo("event-blazor");
        await Assert.That(result.KeycloakClientSecret).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ReadConfigurationAsync_WithEnabledStoredKeycloak_PrefersStoredValues()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakEnabled, true);
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakAuthority, "https://stored.example.test/realms/events");
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakClientId, "stored-blazor");
        var service = CreateService(
            new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://deployment.example.test/realms/events",
                ["Keycloak:ClientId"] = "deployment-blazor"
            },
            repository);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.KeycloakDetectedFromEnvironment).IsFalse();
        await Assert.That(result.KeycloakAuthority).IsEqualTo("https://stored.example.test/realms/events");
        await Assert.That(result.KeycloakClientId).IsEqualTo("stored-blazor");
    }

    [Test]
    public async Task ReadConfigurationAsync_WithDeploymentManagedKeycloak_PrefersDeploymentTuple()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakEnabled, true);
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakAuthority, "https://stored.example.test/realms/events");
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakClientId, "stored-blazor");
        var service = CreateService(
            new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://deployment.example.test/realms/events",
                ["Keycloak:ClientId"] = "deployment-blazor",
                ["Keycloak:ClientSecret"] = "must-not-leave-the-server",
                ["Secrets:Ownership:DeploymentManagedKeys:0"] = GovernanceSettingKeys.Authentication.KeycloakAuthority,
                ["Secrets:Ownership:DeploymentManagedKeys:1"] = GovernanceSettingKeys.Authentication.KeycloakClientId
            },
            repository);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.KeycloakDetectedFromEnvironment).IsTrue();
        await Assert.That(result.KeycloakAuthority).IsEqualTo("https://deployment.example.test/realms/events");
        await Assert.That(result.KeycloakClientId).IsEqualTo("deployment-blazor");
        await Assert.That(result.KeycloakClientSecret).IsEqualTo(string.Empty);
        await Assert.That(await service.IsConfiguredAsync()).IsTrue();
    }

    [Test]
    public async Task IsConfiguredAsync_UsesCompleteDeploymentKeycloakPredicate()
    {
        var complete = CreateService(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://id.example.test/realms/events",
            ["Keycloak:ClientId"] = "event-blazor"
        });
        var partial = CreateService(new Dictionary<string, string?>
        {
            ["Keycloak:Authority"] = "https://id.example.test/realms/events",
            ["Keycloak:Audience"] = "islamu-event-api"
        });

        await Assert.That(await complete.IsConfiguredAsync()).IsTrue();
        await Assert.That(await partial.IsConfiguredAsync()).IsFalse();
    }

    [Test]
    public async Task ReadConfigurationAsync_WithIncompleteStoredKeycloak_FailsClosed()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakEnabled, true);
        ConfigureSetting(
            repository,
            GovernanceSettingKeys.Authentication.KeycloakAuthority,
            "https://stored.example.test/realms/events");
        var service = CreateService(new Dictionary<string, string?>(), repository);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.KeycloakEnabled).IsFalse();
        await Assert.That(await service.IsConfiguredAsync()).IsFalse();
    }

    [Test]
    public async Task ReadConfigurationAsync_WithDeploymentManagedAuthority_PreservesStoredClientId()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakEnabled, true);
        ConfigureSetting(repository, GovernanceSettingKeys.Authentication.KeycloakClientId, "stored-blazor");
        var service = CreateService(
            new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://deployment.example.test/realms/events",
                ["Secrets:Ownership:DeploymentManagedKeys:0"] = GovernanceSettingKeys.Authentication.KeycloakAuthority
            },
            repository);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.KeycloakEnabled).IsTrue();
        await Assert.That(result.KeycloakDetectedFromEnvironment).IsTrue();
        await Assert.That(result.KeycloakAuthority).IsEqualTo("https://deployment.example.test/realms/events");
        await Assert.That(result.KeycloakClientId).IsEqualTo("stored-blazor");
    }

    [Test]
    public async Task ApplyConfigurationAsync_WithDeploymentManagedAuthority_PersistsOnlyClientIdMetadata()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        var service = CreateService(
            new Dictionary<string, string?>
            {
                ["Secrets:Ownership:DeploymentManagedKeys:0"] = GovernanceSettingKeys.Authentication.KeycloakAuthority
            },
            repository);

        await service.ApplyConfigurationAsync(new()
        {
            KeycloakEnabled = true,
            KeycloakAuthority = "https://must-not-be-persisted.example.test/realms/events",
            KeycloakClientId = "application-managed-client"
        });

        await repository.DidNotReceive().UpsertAsync(
            Arg.Is<SystemSetting>(setting =>
                setting.SettingKey == GovernanceSettingKeys.Authentication.KeycloakAuthority),
            Arg.Any<CancellationToken>());
        await repository.Received(1).UpsertAsync(
            Arg.Is<SystemSetting>(setting =>
                setting.SettingKey == GovernanceSettingKeys.Authentication.KeycloakClientId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyConfigurationAsync_WithDeploymentManagedSecret_DoesNotPersistSecret()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        var service = CreateService(
            new Dictionary<string, string?>
            {
                ["Secrets:Ownership:DeploymentManagedKeys:0"] = InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret
            },
            repository);

        await service.ApplyConfigurationAsync(new()
        {
            KeycloakEnabled = true,
            KeycloakAuthority = "https://id.example.test/realms/events",
            KeycloakClientId = "event-blazor",
            KeycloakClientSecret = "must-not-be-persisted"
        });

        await repository.DidNotReceive().UpsertAsync(
            Arg.Is<SystemSetting>(setting =>
                setting.SettingKey == InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadConfigurationWithSecretsAsync_WithDeploymentManagedSecret_IgnoresStoredSecret()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        ConfigureSetting(
            repository,
            InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret,
            "stored-secret");
        var service = CreateService(
            new Dictionary<string, string?>
            {
                ["Keycloak:ClientSecret"] = "deployment-secret",
                ["Secrets:Ownership:DeploymentManagedKeys:0"] = InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret
            },
            repository);

        var result = await service.ReadConfigurationWithSecretsAsync();

        await Assert.That(result.KeycloakClientSecret).IsEqualTo("deployment-secret");
    }

    private static AuthProviderConfigurationService CreateService(
        IReadOnlyDictionary<string, string?> values,
        ISystemSettingRepository? repository = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new AuthProviderConfigurationService(
            repository ?? Substitute.For<ISystemSettingRepository>(),
            configuration);
    }

    private static void ConfigureSetting(ISystemSettingRepository repository, string key, object value)
    {
        repository.GetByKey(key, Arg.Any<CancellationToken>()).Returns(new SystemSetting
        {
            SettingKey = key,
            Value = JsonSerializer.Serialize(value)
        });
    }
}
