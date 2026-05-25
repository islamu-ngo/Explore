// ABOUTME: Tests Cerbos authorization provider configuration persistence and redaction behavior.
// ABOUTME: Verifies Admin API endpoint credentials are write-only and unsafe endpoints fail before storage.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public class AuthorizationProviderConfigurationServiceTests
{
    [Test]
    public async Task ReadConfigurationAsync_RedactsAdminApiCredentialsAndReturnsConfiguredFlags()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateSetting(GovernanceSettingKeys.Security.AuthorizationProvider, "cerbos"));
        repository.GetByKey(GovernanceSettingKeys.Cerbos.GrpcEndpoint)
            .Returns(CreateSetting(GovernanceSettingKeys.Cerbos.GrpcEndpoint, "https://cerbosgrpc.example.com:443"));
        repository.GetByKey(GovernanceSettingKeys.Cerbos.CustomAdminEndpoint)
            .Returns(CreateSetting(GovernanceSettingKeys.Cerbos.CustomAdminEndpoint, "https://tenant-cerbos.example.com:3592"));
        repository.GetByKey(InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername)
            .Returns(CreateSetting(InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername, "admin"));
        repository.GetByKey(InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword)
            .Returns(CreateSetting(InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword, "secret"));
        var service = CreateService(repository);

        var configuration = await service.ReadConfigurationAsync();

        await Assert.That(configuration.CerbosAdminEndpoint).IsEqualTo("https://tenant-cerbos.example.com:3592");
        await Assert.That(configuration.CerbosAdminUsername).IsNull();
        await Assert.That(configuration.CerbosAdminPassword).IsNull();
        await Assert.That(configuration.CerbosAdminUsernameConfigured).IsTrue();
        await Assert.That(configuration.CerbosAdminPasswordConfigured).IsTrue();
    }

    [Test]
    public async Task ReadConfigurationAsync_WhenAdminApiCredentialsComeFromConfiguration_ReturnsConfiguredFlagsWithoutSecrets()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateSetting(GovernanceSettingKeys.Security.AuthorizationProvider, "cerbos"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cerbos:AdminApi:AdminUsername"] = "cerbos",
                ["Cerbos:AdminApi:AdminPassword"] = "server-side-secret"
            })
            .Build();
        var service = CreateService(repository, configuration: configuration);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.CerbosAdminUsername).IsNull();
        await Assert.That(result.CerbosAdminPassword).IsNull();
        await Assert.That(result.CerbosAdminUsernameConfigured).IsTrue();
        await Assert.That(result.CerbosAdminPasswordConfigured).IsTrue();
    }


    [Test]
    public async Task ReadConfigurationAsync_WhenEnvironmentEndpointExists_BootstrapsEditableApplicationManagedMetadata()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cerbos:GrpcEndpoint"] = "cerbosgrpc.openislamu.org:443"
            })
            .Build();
        var service = CreateService(repository, configuration: configuration);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.CerbosGrpcEndpoint).IsEqualTo("cerbosgrpc.openislamu.org:443");
        await Assert.That(result.CerbosEndpointOwnership.Mode).IsEqualTo("application-managed");
        await Assert.That(result.CerbosEndpointOwnership.Source).IsEqualTo("deployment-bootstrap");
        await Assert.That(result.CerbosEndpointOwnership.Badge).IsEqualTo("Bootstrap from Deployment");
        await Assert.That(result.CerbosEndpointOwnership.Editable).IsTrue();
        await Assert.That(result.CerbosEndpointOwnership.BootstrapAvailable).IsTrue();
        await Assert.That(result.CerbosEndpointOwnership.Description)
            .Contains("If you modify them, saved application settings will be used from now on");
    }

    [Test]
    public async Task ReadConfigurationAsync_WhenDatabaseEndpointExists_DatabaseWinsOverEnvironmentBootstrap()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateSetting(GovernanceSettingKeys.Security.AuthorizationProvider, "cerbos"));
        repository.GetByKey(GovernanceSettingKeys.Cerbos.GrpcEndpoint)
            .Returns(CreateSetting(GovernanceSettingKeys.Cerbos.GrpcEndpoint, "https://saved-cerbos.example.com:443"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cerbos:GrpcEndpoint"] = "cerbosgrpc.openislamu.org:443"
            })
            .Build();
        var service = CreateService(repository, configuration: configuration);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.CerbosGrpcEndpoint).IsEqualTo("https://saved-cerbos.example.com:443");
        await Assert.That(result.AuthorizationProviderConfigured).IsTrue();
        await Assert.That(result.CerbosEndpointOwnership.Source).IsEqualTo("application");
        await Assert.That(result.CerbosEndpointOwnership.BootstrapAvailable).IsFalse();
    }

    [Test]
    public async Task ReadConfigurationAsync_WhenEndpointDeploymentManaged_UsesEnvironmentAndReturnsReadOnlyMetadata()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.GetByKey(GovernanceSettingKeys.Cerbos.GrpcEndpoint)
            .Returns(CreateSetting(GovernanceSettingKeys.Cerbos.GrpcEndpoint, "https://saved-cerbos.example.com:443"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cerbos:GrpcEndpoint"] = "cerbosgrpc.openislamu.org:443",
                ["Secrets:Ownership:DeploymentManagedKeys:0"] = GovernanceSettingKeys.Cerbos.GrpcEndpoint
            })
            .Build();
        var service = CreateService(repository, configuration: configuration);

        var result = await service.ReadConfigurationAsync();

        await Assert.That(result.CerbosGrpcEndpoint).IsEqualTo("cerbosgrpc.openislamu.org:443");
        await Assert.That(result.CerbosEndpointOwnership.Mode).IsEqualTo("deployment-managed");
        await Assert.That(result.CerbosEndpointOwnership.Source).IsEqualTo("deployment");
        await Assert.That(result.CerbosEndpointOwnership.Badge).IsEqualTo("Managed by Deployment");
        await Assert.That(result.CerbosEndpointOwnership.Editable).IsFalse();
        await Assert.That(result.CerbosEndpointOwnership.BootstrapAvailable).IsFalse();
    }

    [Test]
    public async Task ApplyConfigurationAsync_WhenEndpointAndCredentialsDeploymentManaged_DoesNotPersistOwnedValues()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.Create(Arg.Any<SystemSetting>()).Returns(call => call.Arg<SystemSetting>());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secrets:Ownership:DeploymentManagedKeys:0"] = GovernanceSettingKeys.Cerbos.GrpcEndpoint,
                ["Secrets:Ownership:DeploymentManagedKeys:1"] = InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername,
                ["Secrets:Ownership:DeploymentManagedKeys:2"] = InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword
            })
            .Build();
        var service = CreateService(repository, configuration: configuration);

        await service.ApplyConfigurationAsync(new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "https://edited-cerbos.example.com:443",
            CerbosAdminUsername = "admin",
            CerbosAdminPassword = "secret"
        });

        await repository.Received(1).Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == GovernanceSettingKeys.Security.AuthorizationProvider));
        await repository.DidNotReceive().Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == GovernanceSettingKeys.Cerbos.GrpcEndpoint
            || x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername
            || x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword));
    }

    [Test]
    public async Task ApplyConfigurationAsync_WithAdminCredentials_StoresEndpointAndSecrets()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.Create(Arg.Any<SystemSetting>()).Returns(call => call.Arg<SystemSetting>());
        var invalidator = Substitute.For<IAuthorizationProviderModeCacheInvalidator>();
        var cerbosConfigResolver = Substitute.For<ICerbosConfigResolver>();
        var service = CreateService(repository, invalidator, cerbosConfigResolver);

        await service.ApplyConfigurationAsync(new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443",
            CerbosAdminEndpoint = "https://tenant-cerbos.example.com/base",
            CerbosAdminUsername = "admin",
            CerbosAdminPassword = "secret"
        });

        await repository.Received(1).Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == GovernanceSettingKeys.Cerbos.CustomAdminEndpoint
            && JsonSerializer.Deserialize<string>(x.Value) == "https://tenant-cerbos.example.com/base"));
        await repository.Received(1).Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername
            && JsonSerializer.Deserialize<string>(x.Value) == "admin"));
        await repository.Received(1).Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword
            && JsonSerializer.Deserialize<string>(x.Value) == "secret"));
        cerbosConfigResolver.Received(1).InvalidateCache();
        invalidator.Received(1).InvalidateInstanceMode();
    }

    [Test]
    public async Task ApplyConfigurationAsync_WithLocalProvider_InvalidatesRuntimeProviderModeCache()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.Create(Arg.Any<SystemSetting>()).Returns(call => call.Arg<SystemSetting>());
        var invalidator = Substitute.For<IAuthorizationProviderModeCacheInvalidator>();
        var cerbosConfigResolver = Substitute.For<ICerbosConfigResolver>();
        var service = CreateService(repository, invalidator, cerbosConfigResolver);

        await service.ApplyConfigurationAsync(new AuthorizationProviderConfigurationDto
        {
            Provider = "local",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443"
        });

        await repository.Received(1).Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == GovernanceSettingKeys.Security.AuthorizationProvider
            && JsonSerializer.Deserialize<string>(x.Value) == "local"));
        await repository.Received(1).Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == GovernanceSettingKeys.Cerbos.GrpcEndpoint
            && JsonSerializer.Deserialize<string>(x.Value) == string.Empty));
        cerbosConfigResolver.Received(1).InvalidateCache();
        invalidator.Received(1).InvalidateInstanceMode();
    }

    [Test]
    public async Task ApplyConfigurationAsync_WhenCredentialsAreOmitted_PreservesExistingSecrets()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        repository.Create(Arg.Any<SystemSetting>()).Returns(call => call.Arg<SystemSetting>());
        var service = CreateService(repository);

        await service.ApplyConfigurationAsync(new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443",
            CerbosAdminEndpoint = "https://tenant-cerbos.example.com"
        });

        await repository.DidNotReceive().Create(Arg.Is<SystemSetting>(x =>
            x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername
            || x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword));
        await repository.DidNotReceive().Update(Arg.Is<SystemSetting>(x =>
            x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername
            || x.SettingKey == InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword));
    }

    [Test]
    public async Task ApplyConfigurationAsync_WithUnsafeAdminEndpoint_DoesNotPersistOrInvalidateProviderModeCache()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        var invalidator = Substitute.For<IAuthorizationProviderModeCacheInvalidator>();
        var cerbosConfigResolver = Substitute.For<ICerbosConfigResolver>();
        var service = CreateService(repository, invalidator, cerbosConfigResolver);

        await Assert.That(async () => await service.ApplyConfigurationAsync(new AuthorizationProviderConfigurationDto
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "https://cerbosgrpc.example.com:443",
                CerbosAdminEndpoint = "https://127.0.0.1:3592"
            }))
            .Throws<InvalidOperationException>();

        await repository.DidNotReceive().Create(Arg.Any<SystemSetting>());
        await repository.DidNotReceive().Update(Arg.Any<SystemSetting>());
        cerbosConfigResolver.DidNotReceive().InvalidateCache(Arg.Any<Guid?>());
        invalidator.DidNotReceive().InvalidateInstanceMode();
    }

    [Test]
    public async Task VerifyCerbosAdminEndpointAsync_WithUnsafeEndpoint_ReturnsFalseWithoutPersistingSecrets()
    {
        var repository = Substitute.For<ISystemSettingRepository>();
        var service = CreateService(repository);

        var result = await service.VerifyCerbosAdminEndpointAsync("https://127.0.0.1:3592");

        await Assert.That(result).IsFalse();
        await repository.DidNotReceive().Create(Arg.Any<SystemSetting>());
        await repository.DidNotReceive().Update(Arg.Any<SystemSetting>());
    }

    private static AuthorizationProviderConfigurationService CreateService(
        ISystemSettingRepository repository,
        IAuthorizationProviderModeCacheInvalidator? invalidator = null,
        ICerbosConfigResolver? cerbosConfigResolver = null,
        IConfiguration? configuration = null)
    {
        configuration ??= new ConfigurationBuilder().Build();
        var options = Options.Create(new CerbosPolicyPackageOptions());
        return new AuthorizationProviderConfigurationService(
            repository,
            configuration,
            new CerbosAdminEndpointValidator(options),
            invalidator ?? Substitute.For<IAuthorizationProviderModeCacheInvalidator>(),
            cerbosConfigResolver ?? Substitute.For<ICerbosConfigResolver>(),
            Substitute.For<ILogger<AuthorizationProviderConfigurationService>>());
    }

    private static SystemSetting CreateSetting(string key, string value)
    {
        return new SystemSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = key,
            Value = JsonSerializer.Serialize(value),
            ValueType = SettingValueType.String,
            IsLocked = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
