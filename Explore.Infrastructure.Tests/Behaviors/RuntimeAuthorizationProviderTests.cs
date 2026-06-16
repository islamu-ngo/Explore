// ABOUTME: Tests runtime authorization provider routing between local RBAC and Cerbos instance mode.
// ABOUTME: Verifies JSON-serialized system settings are honored after provider-mode cache invalidation.

using System.Text.Json;
using Cerbos.Api.V1.Effect;
using Cerbos.Sdk;
using Cerbos.Sdk.Builder;
using Cerbos.Sdk.Response;
using Explore.Application.Authentication;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Infrastructure.Services;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Behaviors;

public class RuntimeAuthorizationProviderTests
{
    private static readonly Guid TestTenantId = Guid.Parse("d1b8e7d4-5c1f-4d1d-9f1b-6f5a6f6e9c21");

    [Test]
    public async Task CheckSettingAccessAsync_WithJsonSerializedCerbosMode_RoutesToCerbosProviderAfterInvalidation()
    {
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.UserId.Returns(Guid.NewGuid());
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var machinePrincipalAccessor = Substitute.For<IMachinePrincipalAccessor>();
        machinePrincipalAccessor.IsMachineCaller.Returns(false);
        machinePrincipalAccessor.Current.Returns((Explore.Application.Authentication.ApiKeyPrincipalContext?)null);

        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var tenantContext = Substitute.For<ITenantContext>();
        var cerbosClient = Substitute.For<ICerbosClient>();
        var cerbosClientFactory = Substitute.For<ICerbosClientFactory>();
        var cerbosResponse = new Cerbos.Api.V1.Response.CheckResourcesResponse();
        cerbosResponse.Results.Add(CreateResultEntry(
            GovernanceSettingKeys.Security.AuthorizationProvider,
            "islamuevent_instance_setting",
            "update",
            Effect.Allow));
        cerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns(new CheckResourcesResponse(cerbosResponse));

        var repository = Substitute.For<ISystemSettingRepository>();
        repository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(new SystemSetting
            {
                Id = Guid.NewGuid(),
                SettingKey = GovernanceSettingKeys.Security.AuthorizationProvider,
                Value = JsonSerializer.Serialize("cerbos"),
                ValueType = SettingValueType.String,
                IsLocked = true,
                CreatedAt = DateTime.UtcNow
            });

        var runtimeProvider = new RuntimeAuthorizationProvider(
            new CerbosAuthorizationService(
                cerbosClient,
                new CerbosPrincipalBuilder(adminContext, machinePrincipalAccessor, Substitute.For<IEventAuthoritySnapshotService>()),
                adminContext,
                machinePrincipalAccessor,
                settingsResolver,
                tenantContext,
                cerbosClientFactory,
                Options.Create(new CerbosSettings { GrpcEndpoint = "http://localhost:3593", PlaintextMode = true }),
                Substitute.For<ILogger<CerbosAuthorizationService>>()),
            new FallbackAuthorizationService(
                adminContext,
                machinePrincipalAccessor,
                Substitute.For<IEventAuthoritySnapshotService>(),
                settingsResolver,
                tenantContext,
                Substitute.For<ILogger<FallbackAuthorizationService>>()),
            Substitute.For<ICerbosConfigResolver>(),
            repository,
            new MemoryCache(new MemoryCacheOptions()),
            Substitute.For<ILogger<RuntimeAuthorizationProvider>>());

        runtimeProvider.InvalidateInstanceMode();

        var result = await runtimeProvider.CheckSettingAccessAsync(
            GovernanceSettingKeys.Security.AuthorizationProvider,
            "update");

        await Assert.That(result).IsTrue();
        await cerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithInstanceCerbosUnavailable_DeniesInsteadOfFallingBackToLocalRbac()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        fixture.SystemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateAuthorizationProviderSetting("cerbos"));
        fixture.CerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_tenant",
            TestTenantId.ToString(),
            "create");

        await Assert.That(result).IsFalse();
        await fixture.CerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithInstanceCerbosUnavailable_AllowsSettingChecksThroughLocalAdminParity()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        fixture.SystemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateAuthorizationProviderSetting("cerbos"));
        fixture.CerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var results = await fixture.RuntimeProvider.IsAllowedBatchAsync(
        [
            new AuthorizationCheck(
                ResourceKinds.InstanceSetting,
                "storage",
                AuthorizationActions.InstanceSettings.Update,
                new Dictionary<string, object> { ["settingKey"] = "storage" }),
            new AuthorizationCheck(
                ResourceKinds.InstanceSetting,
                "storage",
                AuthorizationActions.InstanceSettings.View,
                new Dictionary<string, object> { ["settingKey"] = "storage" })
        ]);

        await Assert.That(results).IsEquivalentTo([true, true]);
        await fixture.CerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithInstanceCerbosUnavailable_KeepsNonSettingChecksFailClosed()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        fixture.SystemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateAuthorizationProviderSetting("cerbos"));
        fixture.CerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_tenant",
            TestTenantId.ToString(),
            "create");

        await Assert.That(result).IsFalse();
        await fixture.CerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithInstanceCerbosMode_UsesLocalAuthorizationForAiConversations()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.SystemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateAuthorizationProviderSetting("cerbos"));

        var results = await fixture.RuntimeProvider.IsAllowedBatchAsync(
        [
            new AuthorizationCheck(
                ResourceKinds.AiConversation,
                "GetAiConversationListQuery",
                AuthorizationActions.AiConversations.View,
                null),
            new AuthorizationCheck(
                ResourceKinds.AiConversation,
                "CreateAiConversationCommand",
                AuthorizationActions.AiConversations.Create,
                null)
        ]);

        await Assert.That(results).IsEquivalentTo([true, true]);
        await fixture.CerbosClient.DidNotReceive().CheckResourcesAsync(
            Arg.Any<CheckResourcesRequest>(),
            Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithInstanceCerbosMode_UsesLocalAuthorizationForEventPreCreate()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.SystemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateAuthorizationProviderSetting("cerbos"));

        var results = await fixture.RuntimeProvider.IsAllowedBatchAsync(
        [
            new AuthorizationCheck(
                ResourceKinds.Event,
                CreateEventCommand.PreCreateResourceId,
                AuthorizationActions.Create,
                new Dictionary<string, object>
                {
                    ["authorizationPhase"] = CreateEventCommand.PreCreateAuthorizationPhase
                })
        ]);

        await Assert.That(results).IsEquivalentTo([true]);
        await fixture.CerbosClient.DidNotReceive().CheckResourcesAsync(
            Arg.Any<CheckResourcesRequest>(),
            Arg.Any<Metadata>());
    }

    [Test]
    [Arguments(nameof(CreateStorageUploadSessionCommand), true)]
    [Arguments("019ecd1d-6b34-7b05-9945-970edd3c1440", false)]
    public async Task IsAllowedBatchAsync_WithInstanceCerbosMode_UsesLocalAuthorizationForStorageUploadSessionGate(
        string resourceId,
        bool includeUploadMetadata)
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.SystemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateAuthorizationProviderSetting("cerbos"));

        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId.ToString()
        };

        if (includeUploadMetadata)
        {
            attributes["purpose"] = "event_image";
            attributes["visibility"] = "public_image";
        }

        var results = await fixture.RuntimeProvider.IsAllowedBatchAsync(
        [
            new AuthorizationCheck(
                ResourceKinds.StorageObject,
                resourceId,
                AuthorizationActions.Create,
                attributes)
        ]);

        await Assert.That(results).IsEquivalentTo([true]);
        await fixture.CerbosClient.DidNotReceive().CheckResourcesAsync(
            Arg.Any<CheckResourcesRequest>(),
            Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedAsync_WhenProviderModeReadFails_UsesCerbosFailClosedPathAndSafeLogMetadata()
    {
        var fixture = CreateRuntimeProviderFixture();
        var secretMessage = "authorization provider setting failed for token abc123";
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        fixture.SystemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns<SystemSetting?>(_ => throw new InvalidOperationException(secretMessage));
        fixture.CerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_tenant",
            TestTenantId.ToString(),
            "create");

        await Assert.That(result).IsFalse();
        await fixture.CerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
        fixture.RuntimeLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => ProviderModeFailureLogStateIsRedacted(state, secretMessage)),
            Arg.Is<Exception?>(ex => ex == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithByoClosedPdpUnavailable_ActivatesSafeModeAndDeniesNonInstanceAdmin()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.AdminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        fixture.CerbosConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(CreateByoConfiguration(CerbosFailureMode.Closed));
        fixture.ByoCerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_category",
            Guid.NewGuid().ToString(),
            "create");

        await Assert.That(result).IsFalse();
        await Assert.That(fixture.LocalProvider.SafeMode).IsTrue();
        await fixture.ByoCerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithByoOpenPdpUnavailable_UsesLocalRbacFallback()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.AdminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        fixture.CerbosConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(CreateByoConfiguration(CerbosFailureMode.Open));
        fixture.ByoCerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_category",
            Guid.NewGuid().ToString(),
            "create");

        await Assert.That(result).IsTrue();
        await Assert.That(fixture.LocalProvider.SafeMode).IsFalse();
        await fixture.ByoCerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WhenByoConfigResolutionFails_ActivatesSafeModeInsteadOfUsingLocalRbac()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.AdminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        fixture.CerbosConfigResolver.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns<CerbosConfiguration?>(_ => throw new InvalidOperationException("tenant-secret resolver failure"));

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_category",
            Guid.NewGuid().ToString(),
            "create");

        await Assert.That(result).IsFalse();
        await Assert.That(fixture.LocalProvider.SafeMode).IsTrue();
        await fixture.CerbosClient.DidNotReceive().CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithByoClosedBlankEndpoint_ActivatesSafeModeInsteadOfInstanceFallback()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns(Guid.NewGuid());
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.AdminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        fixture.CerbosConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new CerbosConfiguration
        {
            Endpoint = string.Empty,
            Mode = CerbosMode.CustomEndpoint,
            FailureMode = CerbosFailureMode.Closed,
            IsInstanceDefault = false
        });
        fixture.ByoCerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_category",
            Guid.NewGuid().ToString(),
            "create");

        await Assert.That(result).IsFalse();
        await Assert.That(fixture.LocalProvider.SafeMode).IsTrue();
        await fixture.CerbosClient.DidNotReceive().CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_WithByoClosedFailure_DeniesMachineCallerAfterSafeModeActivates()
    {
        var fixture = CreateRuntimeProviderFixture();
        fixture.AdminContext.UserId.Returns((Guid?)null);
        fixture.AdminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        fixture.MachinePrincipalAccessor.IsMachineCaller.Returns(true);
        fixture.MachinePrincipalAccessor.Current.Returns(new ApiKeyPrincipalContext(
            KeyId: $"key-{Guid.NewGuid():N}",
            TenantId: TestTenantId,
            OwnerType: ExternalApiKeyOwnerType.Tenant,
            OwnerId: TestTenantId,
            Scopes: [ExternalApiKeyScopes.AdminTenant]));
        fixture.CerbosConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(CreateByoConfiguration(CerbosFailureMode.Closed));
        fixture.ByoCerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns<CheckResourcesResponse>(_ => throw CreateUnavailableRpcException());

        var result = await fixture.RuntimeProvider.IsAllowedAsync(
            "islamuevent_category",
            Guid.NewGuid().ToString(),
            "create",
            new Dictionary<string, object> { ["tenantId"] = TestTenantId });

        await Assert.That(result).IsFalse();
        await Assert.That(fixture.LocalProvider.SafeMode).IsTrue();
        await fixture.ByoCerbosClient.Received(1).CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    private static Cerbos.Api.V1.Response.CheckResourcesResponse.Types.ResultEntry CreateResultEntry(
        string resourceId,
        string resourceKind,
        string action,
        Effect effect)
    {
        var entry = new Cerbos.Api.V1.Response.CheckResourcesResponse.Types.ResultEntry
        {
            Resource = new Cerbos.Api.V1.Response.CheckResourcesResponse.Types.ResultEntry.Types.Resource
            {
                Id = resourceId,
                Kind = resourceKind
            }
        };
        entry.Actions.Add(action, effect);
        return entry;
    }

    private static RuntimeProviderFixture CreateRuntimeProviderFixture()
    {
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var machinePrincipalAccessor = Substitute.For<IMachinePrincipalAccessor>();
        machinePrincipalAccessor.IsMachineCaller.Returns(false);
        machinePrincipalAccessor.Current.Returns((Explore.Application.Authentication.ApiKeyPrincipalContext?)null);

        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TestTenantId);

        var cerbosClient = Substitute.For<ICerbosClient>();
        var byoCerbosClient = Substitute.For<ICerbosClient>();
        var cerbosClientFactory = Substitute.For<ICerbosClientFactory>();
        cerbosClientFactory.GetOrCreate(Arg.Any<string>()).Returns(byoCerbosClient);

        var cerbosConfigResolver = Substitute.For<ICerbosConfigResolver>();
        cerbosConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns((CerbosConfiguration?)null);

        var systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        systemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider)
            .Returns(CreateAuthorizationProviderSetting("local"));

        var localProvider = new FallbackAuthorizationService(
            adminContext,
            machinePrincipalAccessor,
            Substitute.For<IEventAuthoritySnapshotService>(),
            settingsResolver,
            tenantContext,
            Substitute.For<ILogger<FallbackAuthorizationService>>());

        var cerbosProvider = new CerbosAuthorizationService(
            cerbosClient,
            new CerbosPrincipalBuilder(adminContext, machinePrincipalAccessor, Substitute.For<IEventAuthoritySnapshotService>()),
            adminContext,
            machinePrincipalAccessor,
            settingsResolver,
            tenantContext,
            cerbosClientFactory,
            Options.Create(new CerbosSettings { GrpcEndpoint = "http://localhost:3593", PlaintextMode = true }),
            Substitute.For<ILogger<CerbosAuthorizationService>>());

        var runtimeLogger = Substitute.For<ILogger<RuntimeAuthorizationProvider>>();

        var runtimeProvider = new RuntimeAuthorizationProvider(
            cerbosProvider,
            localProvider,
            cerbosConfigResolver,
            systemSettingRepository,
            new MemoryCache(new MemoryCacheOptions()),
            runtimeLogger);

        return new RuntimeProviderFixture(
            runtimeProvider,
            localProvider,
            adminContext,
            cerbosConfigResolver,
            systemSettingRepository,
            cerbosClient,
            byoCerbosClient,
            machinePrincipalAccessor,
            runtimeLogger);
    }

    private static SystemSetting CreateAuthorizationProviderSetting(string provider)
    {
        return new SystemSetting
        {
            Id = Guid.NewGuid(),
            SettingKey = GovernanceSettingKeys.Security.AuthorizationProvider,
            Value = JsonSerializer.Serialize(provider),
            ValueType = SettingValueType.String,
            IsLocked = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static CerbosConfiguration CreateByoConfiguration(CerbosFailureMode failureMode)
    {
        return new CerbosConfiguration
        {
            Endpoint = "https://tenant-cerbos.example:3593",
            Mode = CerbosMode.CustomEndpoint,
            FailureMode = failureMode,
            IsInstanceDefault = false
        };
    }

    private static RpcException CreateUnavailableRpcException()
    {
        return new RpcException(new Status(StatusCode.Unavailable, "tenant-secret unavailable"));
    }

    private static bool ProviderModeFailureLogStateIsRedacted(object state, string exceptionMessage)
    {
        var rendered = state.ToString();
        return rendered is not null
            && rendered.Contains("FailureType=InvalidOperationException", StringComparison.Ordinal)
            && !rendered.Contains(exceptionMessage, StringComparison.Ordinal)
            && !rendered.Contains("abc123", StringComparison.Ordinal);
    }

    private sealed record RuntimeProviderFixture(
        RuntimeAuthorizationProvider RuntimeProvider,
        FallbackAuthorizationService LocalProvider,
        IAdminContext AdminContext,
        ICerbosConfigResolver CerbosConfigResolver,
        ISystemSettingRepository SystemSettingRepository,
        ICerbosClient CerbosClient,
        ICerbosClient ByoCerbosClient,
        IMachinePrincipalAccessor MachinePrincipalAccessor,
        ILogger<RuntimeAuthorizationProvider> RuntimeLogger);
}
