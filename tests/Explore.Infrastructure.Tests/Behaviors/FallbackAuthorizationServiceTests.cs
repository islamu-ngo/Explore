// ABOUTME: Unit tests for FallbackAuthorizationService verifying DB-driven authorization logic.
// ABOUTME: Tests the Instance > Tenant > Organization hierarchy and lock semantics.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Behaviors;

public class FallbackAuthorizationServiceTests
{
    private readonly IAdminContext _adminContext;
    private readonly IMachinePrincipalAccessor _machinePrincipalAccessor;
    private readonly IEventAuthoritySnapshotService _eventAuthoritySnapshotService;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FallbackAuthorizationService> _logger;
    private readonly FallbackAuthorizationService _service;

    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestOrgId = Guid.NewGuid();
    private static readonly Guid TestGroupId = Guid.NewGuid();
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestInstanceId = Guid.NewGuid();
    private static readonly string[] DelegatedWebhookActions =
    [
        AuthorizationActions.Webhooks.View,
        AuthorizationActions.Webhooks.Create,
        AuthorizationActions.Webhooks.Update,
        AuthorizationActions.Webhooks.Delete,
        AuthorizationActions.Webhooks.RotateSecret,
        AuthorizationActions.Webhooks.Test,
        AuthorizationActions.Webhooks.Retry,
        AuthorizationActions.Webhooks.Pause,
        AuthorizationActions.Webhooks.Resume,
        AuthorizationActions.Webhooks.ViewDelivery,
        AuthorizationActions.Webhooks.OpenProviderPortal
    ];
    private static readonly string[] SensitiveWebhookActions =
    [
        AuthorizationActions.Webhooks.ManageProvider,
        AuthorizationActions.Webhooks.ReconcilePublication,
        AuthorizationActions.Webhooks.AbandonPublication,
        AuthorizationActions.Webhooks.ViewPayload,
        AuthorizationActions.Webhooks.BulkReplay
    ];
    private static readonly string[] SupportAccessLifecycleActions =
    [
        AuthorizationActions.SupportAccessSessions.View,
        AuthorizationActions.SupportAccessSessions.List,
        AuthorizationActions.SupportAccessSessions.Start,
        AuthorizationActions.SupportAccessSessions.Stop,
        AuthorizationActions.SupportAccessSessions.ViewAudit,
        AuthorizationActions.SupportAccessSessions.ForceStop
    ];

    public FallbackAuthorizationServiceTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _machinePrincipalAccessor = Substitute.For<IMachinePrincipalAccessor>();
        _eventAuthoritySnapshotService = Substitute.For<IEventAuthoritySnapshotService>();
        _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _logger = Substitute.For<ILogger<FallbackAuthorizationService>>();

        _tenantContext.TenantId.Returns(TestTenantId);
        _machinePrincipalAccessor.IsMachineCaller.Returns(false);
        _machinePrincipalAccessor.Current.Returns((Explore.Application.Authentication.ApiKeyPrincipalContext?)null);

        _service = new FallbackAuthorizationService(
            _adminContext,
            _machinePrincipalAccessor,
            _eventAuthoritySnapshotService,
            _organizationMemberRepository,
            _groupMemberRepository,
            _settingsResolver,
            _tenantContext,
            _logger);
    }

    private static Dictionary<string, object> OrganizationMemberAttributes() => new()
    {
        ["tenantId"] = TestTenantId.ToString(),
        ["organizationId"] = TestOrgId.ToString(),
        ["userId"] = Guid.NewGuid().ToString()
    };

    private static Dictionary<string, object> StorageObjectAttributes(string visibility, Guid? createdBy = null) => new()
    {
        ["tenantId"] = TestTenantId.ToString(),
        ["visibility"] = visibility,
        ["lifecycleState"] = StorageObjectLifecycleStates.Active,
        ["createdBy"] = (createdBy ?? Guid.NewGuid()).ToString("D")
    };

    private static Dictionary<string, object> SupportAccessAttributes(Guid? targetTenantId = null) => new()
    {
        ["tenantId"] = (targetTenantId ?? TestTenantId).ToString("D"),
        ["sessionId"] = Guid.NewGuid().ToString("D"),
        ["actorUserId"] = Guid.NewGuid().ToString("D"),
        ["mode"] = "ReadOnly",
        ["status"] = "Active"
    };

    private static Dictionary<string, object> WebhookOwnerAttributes(
        WebhookConsumerKind ownerKind,
        Guid ownerId)
    {
        var attributes = new Dictionary<string, object>
        {
            ["ownerKindId"] = (int)ownerKind,
            ["ownerId"] = ownerId.ToString("D")
        };

        switch (ownerKind)
        {
            case WebhookConsumerKind.Instance:
                attributes["instanceId"] = ownerId.ToString("D");
                break;
            case WebhookConsumerKind.Tenant:
                attributes["tenantId"] = ownerId.ToString("D");
                break;
            case WebhookConsumerKind.Organization:
                attributes["tenantId"] = TestTenantId.ToString("D");
                attributes["organizationId"] = ownerId.ToString("D");
                break;
            case WebhookConsumerKind.Group:
                attributes["tenantId"] = TestTenantId.ToString("D");
                attributes["groupId"] = ownerId.ToString("D");
                break;
            case WebhookConsumerKind.User:
                attributes["tenantId"] = TestTenantId.ToString("D");
                attributes["userId"] = ownerId.ToString("D");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ownerKind));
        }

        return attributes;
    }

    // === Instance Admin Tests ===

    [Test]
    public async Task IsAllowed_InstanceAdmin_AllowsEverything()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync("islamuevent_instance_setting", "any-key", "update");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_InstanceAdmin_AllowsTenantSettingEvenWhenLocked()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["isLockedByInstance"] = true };
        var result = await _service.IsAllowedAsync("islamuevent_tenant_setting", "locked-key", "update", attrs);

        await Assert.That(result).IsTrue();
    }

    // === Instance Setting Access ===

    [Test]
    public async Task IsAllowed_NonInstanceAdmin_DeniesInstanceSetting()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_instance_setting", "any-key", "update");

        await Assert.That(result).IsFalse();
    }

    // === Tenant Setting Access ===

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsUnlockedTenantSetting()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["isLockedByInstance"] = false
        };

        var result = await _service.IsAllowedAsync("islamuevent_tenant_setting", "unlocked-key", "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_DeniesLockedTenantSetting()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["isLockedByInstance"] = true
        };

        var result = await _service.IsAllowedAsync("islamuevent_tenant_setting", "locked-key", "update", attrs);

        await Assert.That(result).IsFalse();
    }


    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsLockedTenantBrandingDocumentForHandlerValidation()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["documentKey"] = "tenant.branding",
            ["isLockedByInstance"] = true
        };

        var result = await _service.IsAllowedAsync("islamuevent_tenant_setting", "tenant-branding", "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonTenantAdmin_DeniesTenantSetting()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["isLockedByInstance"] = false
        };

        var result = await _service.IsAllowedAsync("islamuevent_tenant_setting", "some-key", "update", attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsTenantViewAndUpdateForResolvedTenant()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["tenantId"] = TestTenantId };

        var viewResult = await _service.IsAllowedAsync(ResourceKinds.Tenant, TestTenantId.ToString(), AuthorizationActions.View, attrs);
        var updateResult = await _service.IsAllowedAsync(ResourceKinds.Tenant, TestTenantId.ToString(), AuthorizationActions.Update, attrs);

        await Assert.That(viewResult).IsTrue();
        await Assert.That(updateResult).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_DeniesTenantUpdateForDifferentTenant()
    {
        var otherTenantId = Guid.NewGuid();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(otherTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["tenantId"] = otherTenantId };

        var result = await _service.IsAllowedAsync(ResourceKinds.Tenant, otherTenantId.ToString(), AuthorizationActions.Update, attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_DeniesTenantCreateAndDelete()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["tenantId"] = TestTenantId };

        var createResult = await _service.IsAllowedAsync(ResourceKinds.Tenant, TestTenantId.ToString(), AuthorizationActions.Create, attrs);
        var deleteResult = await _service.IsAllowedAsync(ResourceKinds.Tenant, TestTenantId.ToString(), AuthorizationActions.Delete, attrs);

        await Assert.That(createResult).IsFalse();
        await Assert.That(deleteResult).IsFalse();
    }

    [Test]
    public async Task IsAllowed_TenantScopedResource_UsesAmbientTenantWhenTenantAttributeIsMissing()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync(
            ResourceKinds.EmailDispatch,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.EmailDispatches.View);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_WebhookTenantAdmin_AllowsWebhookManagementActions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        var attrs = WebhookOwnerAttributes(WebhookConsumerKind.Tenant, TestTenantId);
        string[] actions =
        [
            AuthorizationActions.Webhooks.View,
            AuthorizationActions.Webhooks.Create,
            AuthorizationActions.Webhooks.Update,
            AuthorizationActions.Webhooks.Delete,
            AuthorizationActions.Webhooks.RotateSecret,
            AuthorizationActions.Webhooks.Test,
            AuthorizationActions.Webhooks.Retry,
            AuthorizationActions.Webhooks.RedriveIncoming,
            AuthorizationActions.Webhooks.Pause,
            AuthorizationActions.Webhooks.Resume,
            AuthorizationActions.Webhooks.ReconcilePublication,
            AuthorizationActions.Webhooks.AbandonPublication,
            AuthorizationActions.Webhooks.ViewDelivery,
            AuthorizationActions.Webhooks.ViewPayload,
            AuthorizationActions.Webhooks.BulkReplay,
            AuthorizationActions.Webhooks.ManageProvider,
            AuthorizationActions.Webhooks.OpenProviderPortal
        ];

        foreach (var action in actions)
        {
            var result = await _service.IsAllowedAsync(
                ResourceKinds.Webhook,
                Guid.NewGuid().ToString("D"),
                action,
                attrs);

            await Assert.That(result).IsTrue();
        }
    }

    [Test]
    public async Task IsAllowed_IncomingWebhookWorker_AllowsOnlyDedicatedProcessingAction()
    {
        var principal = new Explore.Application.Authentication.ApiKeyPrincipalContext(
            "internal-webhook-worker",
            TestTenantId,
            Explore.Domain.Enums.ExternalApiKeyOwnerType.Tenant,
            TestTenantId,
            [InternalMachineScopes.ProcessIncomingWebhook]);
        _machinePrincipalAccessor.IsMachineCaller.Returns(true);
        _machinePrincipalAccessor.Current.Returns(principal);
        var attributes = new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString("D") };

        var processAllowed = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.ProcessIncoming,
            attributes);
        var redriveAllowed = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.RedriveIncoming,
            attributes);
        var providerManagementAllowed = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.ManageProvider,
            attributes);
        var pauseAllowed = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.Pause,
            attributes);
        var reconcileAllowed = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.ReconcilePublication,
            attributes);
        var payloadAllowed = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.ViewPayload,
            attributes);
        var bulkReplayAllowed = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.BulkReplay,
            attributes);

        await Assert.That(processAllowed).IsTrue();
        await Assert.That(redriveAllowed).IsFalse();
        await Assert.That(providerManagementAllowed).IsFalse();
        await Assert.That(pauseAllowed).IsFalse();
        await Assert.That(reconcileAllowed).IsFalse();
        await Assert.That(payloadAllowed).IsFalse();
        await Assert.That(bulkReplayAllowed).IsFalse();
    }

    [Test]
    [Arguments(WebhookConsumerKind.Organization)]
    [Arguments(WebhookConsumerKind.Group)]
    [Arguments(WebhookConsumerKind.User)]
    public async Task IsAllowed_DelegatedWebhookOwner_AllowsExactOwnerManagementActions(
        WebhookConsumerKind ownerKind)
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        var ownerId = ownerKind switch
        {
            WebhookConsumerKind.Organization => TestOrgId,
            WebhookConsumerKind.Group => TestGroupId,
            WebhookConsumerKind.User => TestUserId,
            _ => throw new ArgumentOutOfRangeException(nameof(ownerKind))
        };
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.IsGroupAdminAsync(TestGroupId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.UserId.Returns(TestUserId);
        var attrs = WebhookOwnerAttributes(ownerKind, ownerId);

        foreach (var action in DelegatedWebhookActions)
        {
            var result = await _service.IsAllowedAsync(
                ResourceKinds.Webhook,
                Guid.NewGuid().ToString("D"),
                action,
                attrs);

            await Assert.That(result).IsTrue();
        }
    }

    [Test]
    [Arguments(WebhookConsumerKind.Organization)]
    [Arguments(WebhookConsumerKind.Group)]
    [Arguments(WebhookConsumerKind.User)]
    public async Task IsAllowed_DelegatedWebhookOwner_DeniesUnrelatedOwner(
        WebhookConsumerKind ownerKind)
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.UserId.Returns(Guid.NewGuid());
        var ownerId = ownerKind switch
        {
            WebhookConsumerKind.Organization => TestOrgId,
            WebhookConsumerKind.Group => TestGroupId,
            WebhookConsumerKind.User => TestUserId,
            _ => throw new ArgumentOutOfRangeException(nameof(ownerKind))
        };

        var result = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Webhooks.Update,
            WebhookOwnerAttributes(ownerKind, ownerId));

        await Assert.That(result).IsFalse();
    }

    [Test]
    [Arguments(WebhookConsumerKind.Organization)]
    [Arguments(WebhookConsumerKind.Group)]
    [Arguments(WebhookConsumerKind.User)]
    public async Task IsAllowed_DelegatedWebhookOwner_DeniesSensitiveActions(
        WebhookConsumerKind ownerKind)
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.IsGroupAdminAsync(TestGroupId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.UserId.Returns(TestUserId);
        var ownerId = ownerKind switch
        {
            WebhookConsumerKind.Organization => TestOrgId,
            WebhookConsumerKind.Group => TestGroupId,
            WebhookConsumerKind.User => TestUserId,
            _ => throw new ArgumentOutOfRangeException(nameof(ownerKind))
        };
        var attrs = WebhookOwnerAttributes(ownerKind, ownerId);

        foreach (var action in SensitiveWebhookActions)
        {
            var result = await _service.IsAllowedAsync(
                ResourceKinds.Webhook,
                Guid.NewGuid().ToString("D"),
                action,
                attrs);

            await Assert.That(result).IsFalse();
        }
    }

    [Test]
    public async Task IsAllowed_InstanceWebhookOwner_DeniesNonInstanceAdministrator()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync(
            ResourceKinds.Webhook,
            TestInstanceId.ToString("D"),
            AuthorizationActions.Webhooks.Create,
            WebhookOwnerAttributes(WebhookConsumerKind.Instance, TestInstanceId));

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_TenantScopedResource_DeniesExplicitDifferentTenant()
    {
        var otherTenantId = Guid.NewGuid();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(otherTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync(
            ResourceKinds.EmailDispatch,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.EmailDispatches.View,
            new Dictionary<string, object> { ["tenantId"] = otherTenantId.ToString("D") });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowedBatch_TenantScopedResource_RejectsCrossTenantAttributes()
    {
        var otherTenantId = Guid.NewGuid();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var checks = new[]
        {
            new AuthorizationCheck(
                ResourceKinds.EmailDispatch,
                Guid.NewGuid().ToString("D"),
                AuthorizationActions.EmailDispatches.View,
                new Dictionary<string, object> { ["tenantId"] = otherTenantId.ToString("D") }),
            new AuthorizationCheck(
                ResourceKinds.Webhook,
                Guid.NewGuid().ToString("D"),
                AuthorizationActions.Update,
                new Dictionary<string, object> { ["tenantId"] = otherTenantId }),
            new AuthorizationCheck(
                ResourceKinds.Category,
                Guid.NewGuid().ToString("D"),
                AuthorizationActions.Update,
                new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString("D") })
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results[0]).IsFalse();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_CustomPropertyProjection_RequiresExplicitMatchingTenant()
    {
        var otherTenantId = Guid.NewGuid();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var checks = new[]
        {
            new AuthorizationCheck(
                ResourceKinds.CustomPropertyProjection,
                "projection-status",
                AuthorizationActions.CustomPropertyProjections.View),
            new AuthorizationCheck(
                ResourceKinds.CustomPropertyProjection,
                "projection-status",
                AuthorizationActions.CustomPropertyProjections.Update,
                new Dictionary<string, object> { ["tenantId"] = otherTenantId.ToString("D") }),
            new AuthorizationCheck(
                ResourceKinds.CustomPropertyProjection,
                "projection-status",
                AuthorizationActions.CustomPropertyProjections.View,
                new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString("D") })
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results[0]).IsFalse();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowed_CustomPropertyProjection_ForTenantAdmin_RequiresExplicitTenantContext()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var missingTenant = await _service.IsAllowedAsync(
            ResourceKinds.CustomPropertyProjection,
            "projection-status",
            AuthorizationActions.CustomPropertyProjections.View);
        var withTenant = await _service.IsAllowedAsync(
            ResourceKinds.CustomPropertyProjection,
            "projection-status",
            AuthorizationActions.CustomPropertyProjections.View,
            new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString("D") });

        await Assert.That(missingTenant).IsFalse();
        await Assert.That(withTenant).IsTrue();
    }

    [Test]
    public async Task IsAllowed_CustomPropertyProjection_ForTenantAdmin_DeniesDifferentTenant()
    {
        var otherTenantId = Guid.NewGuid();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(otherTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync(
            ResourceKinds.CustomPropertyProjection,
            "projection-status",
            AuthorizationActions.CustomPropertyProjections.Update,
            new Dictionary<string, object> { ["tenantId"] = otherTenantId.ToString("D") });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_StorageObjectView_ForRegularUser_DeniesMetadata()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync(
            ResourceKinds.StorageObject,
            Guid.NewGuid().ToString(),
            AuthorizationActions.StorageObjects.View,
            StorageObjectAttributes(StorageObjectVisibilities.PublicImage));

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowedBatch_StorageObject_MatchesSingleReadBoundary()
    {
        var ownerId = Guid.NewGuid();
        _adminContext.UserId.Returns(ownerId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var checks = new[]
        {
            new AuthorizationCheck(
                ResourceKinds.StorageObject,
                Guid.NewGuid().ToString("D"),
                AuthorizationActions.StorageObjects.View,
                StorageObjectAttributes(StorageObjectVisibilities.PublicImage)),
            new AuthorizationCheck(
                ResourceKinds.StorageObject,
                Guid.NewGuid().ToString("D"),
                AuthorizationActions.StorageObjects.Download,
                StorageObjectAttributes(StorageObjectVisibilities.PublicImage)),
            new AuthorizationCheck(
                ResourceKinds.StorageObject,
                Guid.NewGuid().ToString("D"),
                AuthorizationActions.StorageObjects.PresignedDownload,
                StorageObjectAttributes(StorageObjectVisibilities.PrivateOwner, ownerId))
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results[0]).IsFalse();
        await Assert.That(results[1]).IsTrue();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowed_StorageObjectDownload_ForActiveAuthenticatedTenantObject_AllowsReadActions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var attrs = StorageObjectAttributes(StorageObjectVisibilities.AuthenticatedTenant);
        var download = await _service.IsAllowedAsync(
            ResourceKinds.StorageObject,
            Guid.NewGuid().ToString(),
            AuthorizationActions.StorageObjects.Download,
            attrs);
        var presigned = await _service.IsAllowedAsync(
            ResourceKinds.StorageObject,
            Guid.NewGuid().ToString(),
            AuthorizationActions.StorageObjects.PresignedDownload,
            attrs);

        await Assert.That(download).IsTrue();
        await Assert.That(presigned).IsTrue();
    }

    [Test]
    public async Task IsAllowed_StorageObjectPresignedDownload_ForPrivateOwnerObject_RequiresCreator()
    {
        var ownerId = Guid.NewGuid();
        _adminContext.UserId.Returns(ownerId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var ownerResult = await _service.IsAllowedAsync(
            ResourceKinds.StorageObject,
            Guid.NewGuid().ToString(),
            AuthorizationActions.StorageObjects.PresignedDownload,
            StorageObjectAttributes(StorageObjectVisibilities.PrivateOwner, createdBy: ownerId));
        var otherUserResult = await _service.IsAllowedAsync(
            ResourceKinds.StorageObject,
            Guid.NewGuid().ToString(),
            AuthorizationActions.StorageObjects.PresignedDownload,
            StorageObjectAttributes(StorageObjectVisibilities.PrivateOwner, createdBy: Guid.NewGuid()));

        await Assert.That(ownerResult).IsTrue();
        await Assert.That(otherUserResult).IsFalse();
    }

    [Test]
    public async Task IsAllowed_SupportAccessSession_InstanceAdmin_AllowsLifecycleActions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        foreach (var action in SupportAccessLifecycleActions)
        {
            var result = await _service.IsAllowedAsync(
                ResourceKinds.SupportAccessSession,
                Guid.NewGuid().ToString("D"),
                action,
                SupportAccessAttributes());

            await Assert.That(result)
                .IsTrue()
                .Because($"instance admins must be able to perform support-access action '{action}'.");
        }
    }

    [Test]
    public async Task IsAllowed_SupportAccessSession_TenantAdmin_AllowsEvidenceReadsOnly()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = SupportAccessAttributes();

        var view = await _service.IsAllowedAsync(ResourceKinds.SupportAccessSession, "session-1", AuthorizationActions.SupportAccessSessions.View, attrs);
        var list = await _service.IsAllowedAsync(ResourceKinds.SupportAccessSession, "session-1", AuthorizationActions.SupportAccessSessions.List, attrs);
        var viewAudit = await _service.IsAllowedAsync(ResourceKinds.SupportAccessSession, "session-1", AuthorizationActions.SupportAccessSessions.ViewAudit, attrs);
        var start = await _service.IsAllowedAsync(ResourceKinds.SupportAccessSession, "session-1", AuthorizationActions.SupportAccessSessions.Start, attrs);
        var stop = await _service.IsAllowedAsync(ResourceKinds.SupportAccessSession, "session-1", AuthorizationActions.SupportAccessSessions.Stop, attrs);
        var forceStop = await _service.IsAllowedAsync(ResourceKinds.SupportAccessSession, "session-1", AuthorizationActions.SupportAccessSessions.ForceStop, attrs);

        await Assert.That(view).IsTrue();
        await Assert.That(list).IsTrue();
        await Assert.That(viewAudit).IsTrue();
        await Assert.That(start).IsFalse();
        await Assert.That(stop).IsFalse();
        await Assert.That(forceStop).IsFalse();
    }

    [Test]
    public async Task IsAllowed_SupportAccessSession_OtherTenantAdmin_DeniesTargetTenantEvidence()
    {
        var otherTenantId = Guid.NewGuid();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(otherTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = SupportAccessAttributes(TestTenantId);

        foreach (var action in new[]
                 {
                     AuthorizationActions.SupportAccessSessions.View,
                     AuthorizationActions.SupportAccessSessions.List,
                     AuthorizationActions.SupportAccessSessions.ViewAudit
                 })
        {
            var result = await _service.IsAllowedAsync(
                ResourceKinds.SupportAccessSession,
                Guid.NewGuid().ToString("D"),
                action,
                attrs);

            await Assert.That(result)
                .IsFalse()
                .Because($"tenant admins must not see support-access evidence for another tenant via '{action}'.");
        }
    }

    [Test]
    public async Task IsAllowed_SupportAccessSession_RegularUser_DeniesAllActions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        foreach (var action in SupportAccessLifecycleActions)
        {
            var result = await _service.IsAllowedAsync(
                ResourceKinds.SupportAccessSession,
                Guid.NewGuid().ToString("D"),
                action,
                SupportAccessAttributes());

            await Assert.That(result)
                .IsFalse()
                .Because($"regular users must not perform support-access action '{action}'.");
        }
    }

    [Test]
    public async Task IsAllowedBatch_SupportAccessSession_TenantAdmin_MatchesCerbosEvidenceReadMatrix()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = SupportAccessAttributes();
        var checks = SupportAccessLifecycleActions
            .Select(action => new AuthorizationCheck(
                ResourceKinds.SupportAccessSession,
                Guid.NewGuid().ToString("D"),
                action,
                attrs))
            .ToArray();

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(SupportAccessLifecycleActions.Length);
        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsTrue();
        await Assert.That(results[2]).IsFalse();
        await Assert.That(results[3]).IsFalse();
        await Assert.That(results[4]).IsTrue();
        await Assert.That(results[5]).IsFalse();
    }

    // === Organization Access ===

    [Test]
    public async Task IsAllowed_OrgAdmin_AllowsOrganizationResource()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var result = await _service.IsAllowedAsync("islamuevent_organization", TestOrgId.ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsOrganizationInTheirTenant()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var result = await _service.IsAllowedAsync("islamuevent_organization", TestOrgId.ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonOrgAdmin_DeniesOrganizationResource()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var result = await _service.IsAllowedAsync("islamuevent_organization", TestOrgId.ToString(), "update", attrs);

        await Assert.That(result).IsFalse();
    }

    // === Tenant User Role Grant Access ===

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsTenantUserRoleGrantCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString() };
        var result = await _service.IsAllowedAsync("islamuevent_tenant_user_role_grant", Guid.NewGuid().ToString(), "create", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsTenantUserRoleGrantView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString() };
        var result = await _service.IsAllowedAsync("islamuevent_tenant_user_role_grant", Guid.NewGuid().ToString(), "view", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesTenantUserRoleGrantCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_tenant_user_role_grant", Guid.NewGuid().ToString(), "create");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesTenantUserRoleGrantView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString() };
        var result = await _service.IsAllowedAsync("islamuevent_tenant_user_role_grant", Guid.NewGuid().ToString(), "view", attrs);

        await Assert.That(result).IsFalse();
    }

    // === Organization Member Access ===

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsOrganizationMemberViewAndManageActions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = OrganizationMemberAttributes();

        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "view", attrs)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "create", attrs)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "update", attrs)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "delete", attrs)).IsTrue();
    }

    [Test]
    public async Task IsAllowed_OrganizationAdmin_AllowsOrganizationMemberViewAndManageActions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = OrganizationMemberAttributes();

        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "view", attrs)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "create", attrs)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "update", attrs)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync("islamuevent_organization_member", Guid.NewGuid().ToString(), "delete", attrs)).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesOrganizationMemberView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync(
            "islamuevent_organization_member",
            Guid.NewGuid().ToString(),
            "view",
            OrganizationMemberAttributes());

        await Assert.That(result).IsFalse();
    }

    // === Group Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsGroupView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_group", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_OrgAdmin_AllowsGroupUpdate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var result = await _service.IsAllowedAsync("islamuevent_group", Guid.NewGuid().ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesGroupUpdate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_group", Guid.NewGuid().ToString(), "update");

        await Assert.That(result).IsFalse();
    }

    // === Group Member Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsGroupMemberViewAndCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var viewResult = await _service.IsAllowedAsync("islamuevent_group_member", Guid.NewGuid().ToString(), "view");
        var createResult = await _service.IsAllowedAsync("islamuevent_group_member", Guid.NewGuid().ToString(), "create");

        await Assert.That(viewResult).IsTrue();
        await Assert.That(createResult).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesGroupMemberDelete()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_group_member", Guid.NewGuid().ToString(), "delete");

        await Assert.That(result).IsFalse();
    }

    // === Event-Scoped Resource Context ===

    [Test]
    public async Task IsAllowed_EventChildMissingEventId_DeniesEvenForTenantAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId
        };

        var result = await _service.IsAllowedAsync("islamuevent_event_session", Guid.NewGuid().ToString(), "update", attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventCreateForAuthenticatedUser_AllowsHandlerPolicyEvaluation()
    {
        var userId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["authorizationPhase"] = "pre_create"
        };

        var result = await _service.IsAllowedAsync("islamuevent_event", "create", "create", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_EventCreateWithoutAuthenticatedUser_Denies()
    {
        _adminContext.UserId.Returns((Guid?)null);
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["authorizationPhase"] = "pre_create"
        };

        var result = await _service.IsAllowedAsync("islamuevent_event", "create", "create", attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventCreateWithDifferentTenant_Denies()
    {
        _adminContext.UserId.Returns(Guid.NewGuid());
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = Guid.NewGuid(),
            ["authorizationPhase"] = "pre_create"
        };

        var result = await _service.IsAllowedAsync("islamuevent_event", "create", "create", attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_OrganizationCreateForAuthenticatedUser_AllowsHandlerPolicyEvaluation()
    {
        var userId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["authorizationPhase"] = CreateOrganizationCommand.PreCreateAuthorizationPhase
        };

        var result = await _service.IsAllowedAsync(
            "islamuevent_organization",
            CreateOrganizationCommand.PreCreateResourceId,
            "create",
            attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_OrganizationCreateWithoutAuthenticatedUser_Denies()
    {
        _adminContext.UserId.Returns((Guid?)null);
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["authorizationPhase"] = CreateOrganizationCommand.PreCreateAuthorizationPhase
        };

        var result = await _service.IsAllowedAsync(
            "islamuevent_organization",
            CreateOrganizationCommand.PreCreateResourceId,
            "create",
            attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventChildWithEventContext_AllowsTenantAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = CreateEventContextAttributes();

        var result = await _service.IsAllowedAsync("islamuevent_event_session", Guid.NewGuid().ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_UserOwnedEvent_AllowsOwningUserUpdate()
    {
        var userId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes();
        attrs["userId"] = userId;
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_InstanceAdmin_CanModerateEventButCannotEditWithoutEventAuthority()
    {
        var attrs = CreateEventContextAttributes();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var updateResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, "update", attrs);
        var managementViewResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ViewManagement, attrs);
        var lightModerationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ModerateLight, attrs);
        var heavyModerationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ModerateHeavy, attrs);
        var unmoderationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.Unmoderate, attrs);

        await Assert.That(updateResult).IsFalse();
        await Assert.That(managementViewResult).IsTrue();
        await Assert.That(lightModerationResult).IsTrue();
        await Assert.That(heavyModerationResult).IsTrue();
        await Assert.That(unmoderationResult).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_CanModerateEventButCannotEditWithoutEventAuthority()
    {
        var attrs = CreateEventContextAttributes();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var updateResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, "update", attrs);
        var managementViewResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ViewManagement, attrs);
        var lightModerationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ModerateLight, attrs);
        var heavyModerationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ModerateHeavy, attrs);
        var unmoderationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.Unmoderate, attrs);

        await Assert.That(updateResult).IsFalse();
        await Assert.That(managementViewResult).IsTrue();
        await Assert.That(lightModerationResult).IsTrue();
        await Assert.That(heavyModerationResult).IsTrue();
        await Assert.That(unmoderationResult).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdminWithOrganizationAdminMembership_CanEditOrganizationEvent()
    {
        var attrs = CreateEventContextAttributes();
        attrs["organizationId"] = TestOrgId;
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_OrganizationAdmin_CanEditButCannotModerateEvent()
    {
        var attrs = CreateEventContextAttributes();
        attrs["organizationId"] = TestOrgId;
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);

        var updateResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, "update", attrs);
        var managementViewResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ViewManagement, attrs);
        var lightModerationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ModerateLight, attrs);
        var heavyModerationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.ModerateHeavy, attrs);
        var unmoderationResult = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, AuthorizationActions.Events.Unmoderate, attrs);

        await Assert.That(updateResult).IsTrue();
        await Assert.That(managementViewResult).IsTrue();
        await Assert.That(lightModerationResult).IsFalse();
        await Assert.That(heavyModerationResult).IsFalse();
        await Assert.That(unmoderationResult).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventRoleViewPermission_AllowsManagementView()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes(eventId);
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, eventId, PermissionCodes.EventView);

        var result = await _service.IsAllowedAsync("islamuevent_event", eventId.ToString(), AuthorizationActions.Events.ViewManagement, attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_RegularUserWithoutEventAuthority_DeniesManagementView()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes(eventId);
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, eventId);

        var result = await _service.IsAllowedAsync("islamuevent_event", eventId.ToString(), AuthorizationActions.Events.ViewManagement, attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_UserOwnedEvent_DeniesDifferentUserUpdate()
    {
        var currentUserId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes();
        attrs["userId"] = Guid.NewGuid();
        _adminContext.UserId.Returns(currentUserId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(currentUserId, (Guid)attrs["eventId"], PermissionCodes.EventSessionUpdate);

        var result = await _service.IsAllowedAsync("islamuevent_event", attrs["eventId"]!.ToString()!, "update", attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventChildWithRolePermission_AllowsNonAdmin()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, eventId, PermissionCodes.EventSessionUpdate);

        var attrs = CreateEventContextAttributes(eventId);

        var result = await _service.IsAllowedAsync("islamuevent_event_session", Guid.NewGuid().ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_EventChildWithoutRolePermission_DeniesNonAdmin()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, eventId, PermissionCodes.EventSessionUpdate);

        var attrs = CreateEventContextAttributes(eventId);

        var result = await _service.IsAllowedAsync("islamuevent_event_session", Guid.NewGuid().ToString(), "delete", attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventRegistrationCreateMissingEventId_Denies()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = TestTenantId,
            ["eventSessionId"] = Guid.NewGuid()
        };

        var result = await _service.IsAllowedAsync("islamuevent_event_registration", Guid.NewGuid().ToString(), "create", attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventRegistrationCreateWithEventContext_AllowsAuthenticatedUser()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var attrs = CreateEventContextAttributes();
        attrs["eventSessionId"] = Guid.NewGuid();

        var result = await _service.IsAllowedAsync("islamuevent_event_registration", Guid.NewGuid().ToString(), "create", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_EventRegistrationUpdateWithRolePermission_AllowsNonAdmin()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, eventId, "event_registration:update");

        var attrs = CreateEventContextAttributes(eventId);
        attrs["eventSessionId"] = Guid.NewGuid();

        var result = await _service.IsAllowedAsync("islamuevent_event_registration", Guid.NewGuid().ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_EventRegistrationOwnerCanDeleteButCannotUpdate()
    {
        var userId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes();
        attrs["eventSessionId"] = Guid.NewGuid();
        attrs["userId"] = userId;
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var resourceId = Guid.NewGuid().ToString("D");
        var deleteResult = await _service.IsAllowedAsync(
            ResourceKinds.EventRegistration,
            resourceId,
            AuthorizationActions.Delete,
            attrs);
        var updateResult = await _service.IsAllowedAsync(
            ResourceKinds.EventRegistration,
            resourceId,
            AuthorizationActions.Update,
            attrs);

        await Assert.That(deleteResult).IsTrue();
        await Assert.That(updateResult).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventRegistrationDifferentUserCannotDelete()
    {
        var attrs = CreateEventContextAttributes();
        attrs["eventSessionId"] = Guid.NewGuid();
        attrs["userId"] = Guid.NewGuid();
        _adminContext.UserId.Returns(Guid.NewGuid());
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync(
            ResourceKinds.EventRegistration,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Delete,
            attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventRegistrationOwnerCannotDeleteAcrossTenantBoundary()
    {
        var userId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes();
        attrs["tenantId"] = Guid.NewGuid();
        attrs["eventSessionId"] = Guid.NewGuid();
        attrs["userId"] = userId;
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync(
            ResourceKinds.EventRegistration,
            Guid.NewGuid().ToString("D"),
            AuthorizationActions.Delete,
            attrs);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventDayWithRolePermission_AllowsNonAdmin()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, eventId, PermissionCodes.EventDayUpdate);

        var result = await _service.IsAllowedAsync(
            "islamuevent_event_day",
            Guid.NewGuid().ToString(),
            "update",
            CreateEventContextAttributes(eventId));

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_EventAgendaItemWithRolePermission_AllowsNonAdmin()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, eventId, PermissionCodes.EventAgendaItemUpdate);

        var result = await _service.IsAllowedAsync(
            "islamuevent_event_agenda_item",
            Guid.NewGuid().ToString(),
            "update",
            CreateEventContextAttributes(eventId));

        await Assert.That(result).IsTrue();
    }

    // === Custom Property Definition Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsCustomPropertyDefinitionView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_custom_property_definition", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsCustomPropertyDefinitionCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync("islamuevent_custom_property_definition", Guid.NewGuid().ToString(), "create");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesCustomPropertyDefinitionCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_custom_property_definition", Guid.NewGuid().ToString(), "create");

        await Assert.That(result).IsFalse();
    }

    // === Event Contact Share Consent Access ===

    [Test]
    public async Task IsAllowed_OrgAdmin_AllowsContactShareConsentViewAndExport()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var viewResult = await _service.IsAllowedAsync("islamuevent_event_contact_share_consent", Guid.NewGuid().ToString(), "viewsharedcontacts", attrs);
        var exportResult = await _service.IsAllowedAsync("islamuevent_event_contact_share_consent", Guid.NewGuid().ToString(), "exportsharedcontacts", attrs);

        await Assert.That(viewResult).IsTrue();
        await Assert.That(exportResult).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesContactShareConsent()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_event_contact_share_consent", Guid.NewGuid().ToString(), "viewsharedcontacts");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_ContactShareConsent_DeniesUnknownAction()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync("islamuevent_event_contact_share_consent", Guid.NewGuid().ToString(), "delete");

        await Assert.That(result).IsFalse();
    }

    // === Notification Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsNotificationCrud()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var viewResult = await _service.IsAllowedAsync("islamuevent_notification", Guid.NewGuid().ToString(), "view");
        var deleteResult = await _service.IsAllowedAsync("islamuevent_notification", Guid.NewGuid().ToString(), "delete");

        await Assert.That(viewResult).IsTrue();
        await Assert.That(deleteResult).IsTrue();
    }

    // === Actor Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsActorView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_actor", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesActorUpdate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_actor", Guid.NewGuid().ToString(), "update");

        await Assert.That(result).IsFalse();
    }

    // === SafeMode Tests ===

    [Test]
    public async Task IsAllowed_SafeMode_DeniesNonInstanceAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _service.ActivateSafeMode();

        var result = await _service.IsAllowedAsync("islamuevent_event", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_SafeMode_AllowsInstanceAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _service.ActivateSafeMode();

        var result = await _service.IsAllowedAsync("islamuevent_event", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    // === Batch Optimization Tests ===


    [Test]
    public async Task IsAllowedBatch_TenantAdmin_AllowsLockedTenantBrandingDocumentForHandlerValidation()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var checks = new List<AuthorizationCheck>
        {
            new(
                "islamuevent_tenant_setting",
                "tenant-branding",
                "update",
                new Dictionary<string, object>
                {
                    ["tenantId"] = TestTenantId,
                    ["documentKey"] = "tenant.branding",
                    ["isLockedByInstance"] = true
                }),
            new("islamuevent_group", Guid.NewGuid().ToString(), "view"),
            new("islamuevent_group_member", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results[0]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_ReturnsCorrectResults_ForMixedChecks()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view"),
            new("islamuevent_tenant_user_role_grant", Guid.NewGuid().ToString(), "create"),
            new("islamuevent_instance_setting", "key", "update"),
            new("islamuevent_group", Guid.NewGuid().ToString(), "view"),
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(4);
        await Assert.That(results[0]).IsTrue();  // notification: all authenticated
        await Assert.That(results[1]).IsTrue();  // tenant_user_role_grant: tenant admin
        await Assert.That(results[2]).IsFalse(); // instance_setting: only instance admin
        await Assert.That(results[3]).IsTrue();  // group view: all authenticated
    }

    [Test]
    public async Task IsAllowedBatch_EventChildMissingEventContext_Denies()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var checks = new List<AuthorizationCheck>
        {
            new(
                "islamuevent_event_session",
                Guid.NewGuid().ToString(),
                "update",
                new Dictionary<string, object> { ["tenantId"] = TestTenantId }),
            new(
                "islamuevent_event_session",
                Guid.NewGuid().ToString(),
                "update",
                CreateEventContextAttributes())
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0]).IsFalse();
        await Assert.That(results[1]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_EventChildDifferentTenant_DeniesTenantAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var attrs = new Dictionary<string, object>
        {
            ["tenantId"] = Guid.NewGuid(),
            ["eventId"] = Guid.NewGuid()
        };

        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event_session", Guid.NewGuid().ToString(), "update", attrs),
            new("islamuevent_event_registration", Guid.NewGuid().ToString(), "create", attrs),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsFalse();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_UserOwnedEvent_AllowsOwningUserLifecycleActions()
    {
        var userId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes();
        attrs["userId"] = userId;
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var resourceId = attrs["eventId"]!.ToString()!;
        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event", resourceId, "update", attrs),
            new("islamuevent_event", resourceId, "delete", attrs),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsTrue();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_EventRegistrationOwnerCanDeleteButCannotUpdate()
    {
        var userId = Guid.NewGuid();
        var attrs = CreateEventContextAttributes();
        attrs["eventSessionId"] = Guid.NewGuid();
        attrs["userId"] = userId;
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var resourceId = Guid.NewGuid().ToString("D");
        var checks = new List<AuthorizationCheck>
        {
            new(ResourceKinds.EventRegistration, resourceId, AuthorizationActions.Delete, attrs),
            new(ResourceKinds.EventRegistration, resourceId, AuthorizationActions.Update, attrs),
            new(ResourceKinds.Notification, Guid.NewGuid().ToString("D"), AuthorizationActions.View)
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_InstanceAdmin_CanModerateEventButCannotEditWithoutEventAuthority()
    {
        var attrs = CreateEventContextAttributes();
        var resourceId = attrs["eventId"]!.ToString()!;
        _adminContext.UserId.Returns((Guid?)null);
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event", resourceId, "update", attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.ModerateLight, attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.ModerateHeavy, attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.Unmoderate, attrs),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(5);
        await Assert.That(results[0]).IsFalse();
        await Assert.That(results[1]).IsTrue();
        await Assert.That(results[2]).IsTrue();
        await Assert.That(results[3]).IsTrue();
        await Assert.That(results[4]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_TenantAdmin_CanModerateEventButCannotEditWithoutEventAuthority()
    {
        var attrs = CreateEventContextAttributes();
        var resourceId = attrs["eventId"]!.ToString()!;
        _adminContext.UserId.Returns((Guid?)null);
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event", resourceId, "update", attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.ModerateLight, attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.ModerateHeavy, attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.Unmoderate, attrs),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(5);
        await Assert.That(results[0]).IsFalse();
        await Assert.That(results[1]).IsTrue();
        await Assert.That(results[2]).IsTrue();
        await Assert.That(results[3]).IsTrue();
        await Assert.That(results[4]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_OrganizationAdmin_CanEditButCannotModerateEvent()
    {
        var attrs = CreateEventContextAttributes();
        attrs["organizationId"] = TestOrgId;
        var resourceId = attrs["eventId"]!.ToString()!;
        _adminContext.UserId.Returns((Guid?)null);
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([TestOrgId]);

        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event", resourceId, "update", attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.ModerateLight, attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.ModerateHeavy, attrs),
            new("islamuevent_event", resourceId, AuthorizationActions.Events.Unmoderate, attrs),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(5);
        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsFalse();
        await Assert.That(results[3]).IsFalse();
        await Assert.That(results[4]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_EventChildWithRolePermission_AllowsMatchingPermissionOnly()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        ConfigureEventAuthority(userId, eventId, PermissionCodes.EventSessionUpdate);

        var attrs = CreateEventContextAttributes(eventId);
        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event_session", Guid.NewGuid().ToString(), "update", attrs),
            new("islamuevent_event_session", Guid.NewGuid().ToString(), "delete", attrs),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatch_EventRoleSnapshot_IsResolvedOnceForOptimizedBatch()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        ConfigureEventAuthority(userId, eventId, PermissionCodes.EventSessionUpdate);

        var attrs = CreateEventContextAttributes(eventId);
        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event_session", Guid.NewGuid().ToString(), "update", attrs),
            new("islamuevent_event_session", Guid.NewGuid().ToString(), "delete", attrs),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        await _service.IsAllowedBatchAsync(checks);

        await _eventAuthoritySnapshotService.Received(1).GetForUserAndEventsAsync(
            TestTenantId,
            userId,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(eventId)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IsAllowedBatch_EventRolePermissionForDifferentEvent_DoesNotAuthorizeOtherEvent()
    {
        var userId = Guid.NewGuid();
        var authorizedEventId = Guid.NewGuid();
        var otherEventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        ConfigureEventAuthority(userId, authorizedEventId, PermissionCodes.EventSessionUpdate);

        var checks = new List<AuthorizationCheck>
        {
            new("islamuevent_event_session", Guid.NewGuid().ToString(), "update", CreateEventContextAttributes(authorizedEventId)),
            new("islamuevent_event_session", Guid.NewGuid().ToString(), "update", CreateEventContextAttributes(otherEventId)),
            new("islamuevent_notification", Guid.NewGuid().ToString(), "view")
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsTrue();
    }

    [Test]
    public async Task IsAllowed_EventFuturePhaseActions_DeniedEvenWithDirectOrganizationAuthority()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);
        var attributes = CreateEventContextAttributes();
        attributes["organizationId"] = TestOrgId;

        foreach (var action in new[]
        {
            AuthorizationActions.Events.ManageRegistrations,
            AuthorizationActions.Events.ManageTickets,
            AuthorizationActions.Events.ManageAttendees
        })
        {
            await Assert.That(await _service.IsAllowedAsync(
                ResourceKinds.Event,
                attributes["eventId"].ToString()!,
                action,
                attributes)).IsFalse();
        }
    }

    [Test]
    public async Task IsAllowed_ManageRegistrations_AllowsVerifiedOrganizerControllersAndAssignedRole()
    {
        var userId = Guid.NewGuid();
        var personalEventId = Guid.NewGuid();
        var organizationEventId = Guid.NewGuid();
        var assignedEventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _organizationMemberRepository.HasPermissionInOrganization(TestOrgId, userId, PermissionCodes.EventCreate)
            .Returns(true);
        ConfigureEventAuthority(userId, assignedEventId, PermissionCodes.EventRegistrationManage);

        var personalAttributes = CreateVerifiedOrganizerAttributes(personalEventId, userId: userId);
        var organizationAttributes = CreateVerifiedOrganizerAttributes(organizationEventId, organizationId: TestOrgId);
        var assignedAttributes = CreateEventContextAttributes(assignedEventId);

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            personalEventId.ToString(),
            AuthorizationActions.Events.ManageRegistrations,
            personalAttributes)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            organizationEventId.ToString(),
            AuthorizationActions.Events.ManageRegistrations,
            organizationAttributes)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            assignedEventId.ToString(),
            AuthorizationActions.Events.ManageRegistrations,
            assignedAttributes)).IsTrue();
    }

    [Test]
    public async Task IsAllowed_ManageRegistrations_DeniesContributorUnrelatedControllerAndAdminBypasses()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var communityContributor = CreateEventContextAttributes(eventId);
        communityContributor["actorId"] = Guid.NewGuid();
        communityContributor["userId"] = userId;
        var unrelatedController = CreateVerifiedOrganizerAttributes(eventId, userId: userId);
        unrelatedController["organizerUserId"] = Guid.NewGuid();

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageRegistrations,
            communityContributor)).IsFalse();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageRegistrations,
            unrelatedController)).IsFalse();

        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageRegistrations,
            CreateEventContextAttributes(eventId))).IsFalse();

        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageRegistrations,
            CreateEventContextAttributes(eventId))).IsFalse();
    }

    [Test]
    public async Task IsAllowedBatch_ManageRegistrations_MatchesSingleDecisionBoundaries()
    {
        var userId = Guid.NewGuid();
        var organizerEventId = Guid.NewGuid();
        var assignedEventId = Guid.NewGuid();
        var communityEventId = Guid.NewGuid();
        var unrelatedEventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminGroupIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([]);
        ConfigureEventAuthority(userId, assignedEventId, PermissionCodes.EventRegistrationManage);

        var communityAttributes = CreateEventContextAttributes(communityEventId);
        communityAttributes["actorId"] = Guid.NewGuid();
        communityAttributes["userId"] = userId;
        var unrelatedAttributes = CreateVerifiedOrganizerAttributes(unrelatedEventId, userId: userId);
        unrelatedAttributes["organizerUserId"] = Guid.NewGuid();

        var results = await _service.IsAllowedBatchAsync(
        [
            new(ResourceKinds.Event, organizerEventId.ToString(), AuthorizationActions.Events.ManageRegistrations, CreateVerifiedOrganizerAttributes(organizerEventId, userId: userId)),
            new(ResourceKinds.Event, assignedEventId.ToString(), AuthorizationActions.Events.ManageRegistrations, CreateEventContextAttributes(assignedEventId)),
            new(ResourceKinds.Event, communityEventId.ToString(), AuthorizationActions.Events.ManageRegistrations, communityAttributes),
            new(ResourceKinds.Event, unrelatedEventId.ToString(), AuthorizationActions.Events.ManageRegistrations, unrelatedAttributes)
        ]);

        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsTrue();
        await Assert.That(results[2]).IsFalse();
        await Assert.That(results[3]).IsFalse();
    }

    [Test]
    public async Task IsAllowedBatch_ManageRegistrations_DeniesMachineCaller()
    {
        var eventId = Guid.NewGuid();
        _machinePrincipalAccessor.IsMachineCaller.Returns(true);

        var results = await _service.IsAllowedBatchAsync(
        [
            new(ResourceKinds.Event, eventId.ToString(), AuthorizationActions.Events.ManageRegistrations, CreateVerifiedOrganizerAttributes(eventId, userId: TestUserId)),
            new(ResourceKinds.Event, Guid.NewGuid().ToString(), AuthorizationActions.Events.ManageRegistrations, CreateEventContextAttributes()),
            new(ResourceKinds.Event, Guid.NewGuid().ToString(), AuthorizationActions.Events.ManageRegistrations, CreateEventContextAttributes())
        ]);

        await Assert.That(results).IsEquivalentTo([false, false, false]);
    }

    [Test]
    public async Task IsAllowed_EventUnknownAction_DeniedBeforeInstanceAdminWildcard()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        var attributes = CreateEventContextAttributes();

        var result = await _service.IsAllowedAsync(
            ResourceKinds.Event,
            attributes["eventId"].ToString()!,
            "unsupported-action",
            attributes);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowedBatch_EventFuturePhaseActions_DeniedBeforeInstanceAdminWildcard()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        var attributes = CreateEventContextAttributes();
        var resourceId = attributes["eventId"].ToString()!;
        var checks = new List<AuthorizationCheck>
        {
            new(ResourceKinds.Event, resourceId, AuthorizationActions.View, attributes),
            new(ResourceKinds.Event, resourceId, AuthorizationActions.Events.ManageRegistrations, attributes),
            new(ResourceKinds.Event, resourceId, AuthorizationActions.Events.ManageTickets, attributes),
            new(ResourceKinds.Event, resourceId, AuthorizationActions.Events.ManageAttendees, attributes)
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsFalse();
        await Assert.That(results[2]).IsFalse();
        await Assert.That(results[3]).IsFalse();
    }

    [Test]
    public async Task IsAllowed_OrganizerClaimUsesExplicitActionCatalog()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        var attributes = CreateEventContextAttributes();
        attributes["claimId"] = Guid.NewGuid();
        var resourceId = attributes["eventId"].ToString()!;

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            resourceId,
            AuthorizationActions.Events.ReviewOrganizerClaim,
            attributes)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            resourceId,
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            attributes)).IsFalse();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            resourceId,
            AuthorizationActions.Events.ManageAttendees,
            attributes)).IsFalse();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            resourceId,
            AuthorizationActions.Events.ManagePublicActions,
            attributes)).IsFalse();
    }

    [Test]
    public async Task IsAllowed_WithdrawOrganizerClaim_RequiresClaimantActorControl()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.UserId.Returns(TestUserId);
        var attributes = CreateEventContextAttributes();
        attributes["claimId"] = Guid.NewGuid();

        attributes["claimantUserId"] = TestUserId;
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            attributes["claimId"].ToString()!,
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            attributes)).IsTrue();

        attributes.Remove("claimantUserId");
        attributes["claimantOrganizationId"] = TestOrgId;
        _organizationMemberRepository.HasPermissionInOrganization(
                TestOrgId,
                TestUserId,
                PermissionCodes.EventCreate)
            .Returns(true);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            attributes["claimId"].ToString()!,
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            attributes)).IsTrue();

        attributes.Remove("claimantOrganizationId");
        attributes["claimantGroupId"] = TestGroupId;
        _groupMemberRepository.HasPermissionInGroup(
                TestGroupId,
                TestUserId,
                PermissionCodes.EventCreate)
            .Returns(true);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            attributes["claimId"].ToString()!,
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            attributes)).IsTrue();

        _groupMemberRepository.HasPermissionInGroup(
                TestGroupId,
                TestUserId,
                PermissionCodes.EventCreate)
            .Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            attributes["claimId"].ToString()!,
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            attributes)).IsFalse();

        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        attributes.Remove("claimantGroupId");
        attributes["claimantUserId"] = TestUserId;
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            attributes["claimId"].ToString()!,
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            attributes)).IsFalse();
    }

    [Test]
    public async Task IsAllowed_WithdrawOrganizerClaim_DeniesEventCreateMemberForUnrelatedClaimantActor()
    {
        var unrelatedOrganizationId = Guid.NewGuid();
        var unrelatedGroupId = Guid.NewGuid();
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.UserId.Returns(TestUserId);
        _organizationMemberRepository.HasPermissionInOrganization(
                unrelatedOrganizationId,
                TestUserId,
                PermissionCodes.EventCreate)
            .Returns(true);
        _groupMemberRepository.HasPermissionInGroup(
                unrelatedGroupId,
                TestUserId,
                PermissionCodes.EventCreate)
            .Returns(true);
        var organizationClaim = CreateEventContextAttributes();
        organizationClaim["claimantOrganizationId"] = TestOrgId;
        var groupClaim = CreateEventContextAttributes();
        groupClaim["claimantGroupId"] = TestGroupId;

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            Guid.NewGuid().ToString(),
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            organizationClaim)).IsFalse();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            Guid.NewGuid().ToString(),
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            groupClaim)).IsFalse();
    }

    [Test]
    public async Task IsAllowedBatch_WithdrawOrganizerClaim_UsesClaimantOwnerProfileOnly()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.UserId.Returns(TestUserId);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminGroupIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
                TestUserId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([TestOrgId]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(
                TestUserId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([TestGroupId]);
        var personal = CreateEventContextAttributes();
        personal["claimantUserId"] = TestUserId;
        var organization = CreateEventContextAttributes();
        organization["claimantOrganizationId"] = TestOrgId;
        var group = CreateEventContextAttributes();
        group["claimantGroupId"] = TestGroupId;
        var unrelatedOrganization = CreateEventContextAttributes();
        unrelatedOrganization["claimantOrganizationId"] = Guid.NewGuid();
        var unrelatedGroup = CreateEventContextAttributes();
        unrelatedGroup["claimantGroupId"] = Guid.NewGuid();

        var results = await _service.IsAllowedBatchAsync(
        [
            new(ResourceKinds.EventOrganizerClaim, Guid.NewGuid().ToString(), AuthorizationActions.Events.WithdrawOrganizerClaim, personal),
            new(ResourceKinds.EventOrganizerClaim, Guid.NewGuid().ToString(), AuthorizationActions.Events.WithdrawOrganizerClaim, organization),
            new(ResourceKinds.EventOrganizerClaim, Guid.NewGuid().ToString(), AuthorizationActions.Events.WithdrawOrganizerClaim, group),
            new(ResourceKinds.EventOrganizerClaim, Guid.NewGuid().ToString(), AuthorizationActions.Events.WithdrawOrganizerClaim, unrelatedOrganization),
            new(ResourceKinds.EventOrganizerClaim, Guid.NewGuid().ToString(), AuthorizationActions.Events.WithdrawOrganizerClaim, unrelatedGroup)
        ]);

        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsTrue();
        await Assert.That(results[2]).IsTrue();
        await Assert.That(results[3]).IsFalse();
        await Assert.That(results[4]).IsFalse();
    }

    [Test]
    public async Task IsAllowed_EventRolePermissionsMatchPublicActionAndOrganizerClaimPolicy()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(
            userId,
            eventId,
            PermissionCodes.EventManagePublicActions,
            PermissionCodes.EventViewOrganizerClaims);
        var attributes = CreateEventContextAttributes(eventId);
        attributes["claimId"] = Guid.NewGuid();

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManagePublicActions,
            attributes)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            eventId.ToString(),
            AuthorizationActions.Events.ViewOrganizerClaims,
            attributes)).IsTrue();
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.EventOrganizerClaim,
            eventId.ToString(),
            AuthorizationActions.Events.ReviewOrganizerClaim,
            attributes)).IsFalse();
    }

    [Test]
    public async Task IsAllowedBatch_TenantAdminCanViewAndReviewOrganizerClaims()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        var attributes = CreateEventContextAttributes();
        attributes["claimId"] = Guid.NewGuid();
        var eventId = attributes["eventId"].ToString()!;
        var checks = new[]
        {
            new AuthorizationCheck(ResourceKinds.EventOrganizerClaim, eventId, AuthorizationActions.Events.ViewOrganizerClaims, attributes),
            new AuthorizationCheck(ResourceKinds.EventOrganizerClaim, eventId, AuthorizationActions.Events.ReviewOrganizerClaim, attributes)
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results[0]).IsTrue();
        await Assert.That(results[1]).IsTrue();
    }

    [Test]
    public async Task IsAllowed_EventResourceDeniesOrganizerClaimActions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        var attributes = CreateEventContextAttributes();
        var resourceId = attributes["eventId"].ToString()!;

        foreach (var action in new[]
        {
            AuthorizationActions.Events.ClaimOrganizer,
            AuthorizationActions.Events.WithdrawOrganizerClaim,
            AuthorizationActions.Events.ViewOrganizerClaims,
            AuthorizationActions.Events.ReviewOrganizerClaim
        })
        {
            await Assert.That(await _service.IsAllowedAsync(
                ResourceKinds.Event,
                resourceId,
                action,
                attributes)).IsFalse();
        }
    }

    [Test]
    public async Task IsAllowedBatch_MachineCallerDeniesOrganizerClaimsBeforeAdminWildcard()
    {
        _machinePrincipalAccessor.IsMachineCaller.Returns(true);
        _machinePrincipalAccessor.Current.Returns(new Explore.Application.Authentication.ApiKeyPrincipalContext(
            "instance-admin-key",
            null,
            Explore.Domain.Enums.ExternalApiKeyOwnerType.InstanceAdmin,
            TestInstanceId,
            [ExternalApiKeyScopes.AdminInstance]));
        var attributes = CreateEventContextAttributes();
        var resourceId = attributes["eventId"].ToString()!;
        var checks = new[]
        {
            new AuthorizationCheck(ResourceKinds.EventOrganizerClaim, resourceId, AuthorizationActions.Events.ClaimOrganizer, attributes),
            new AuthorizationCheck(ResourceKinds.EventOrganizerClaim, resourceId, AuthorizationActions.Events.WithdrawOrganizerClaim, attributes),
            new AuthorizationCheck(ResourceKinds.EventOrganizerClaim, resourceId, AuthorizationActions.Events.ViewOrganizerClaims, attributes),
            new AuthorizationCheck(ResourceKinds.EventOrganizerClaim, resourceId, AuthorizationActions.Events.ReviewOrganizerClaim, attributes)
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        foreach (var result in results)
        {
            await Assert.That(result).IsFalse();
        }
    }

    // === Unknown Resource Kind ===

    [Test]
    public async Task IsAllowed_UnknownResourceKind_DeniedByDefault()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("unknown_resource", "id", "action");

        await Assert.That(result).IsFalse();
    }

    // === CheckSettingAccess Convenience Method ===

    [Test]
    public async Task CheckSettingAccess_InstanceScope_ChecksInstanceAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.CheckSettingAccessAsync("deployment.mode", "update");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CheckSettingAccess_TenantScope_ChecksLockAndTenantAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _settingsResolver.ResolveWithMetadataAsync("events.require_approval", Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedSetting { Key = "events.require_approval", IsLocked = false });

        var result = await _service.CheckSettingAccessAsync("events.require_approval", "update", tenantId: TestTenantId);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CheckSettingAccess_TenantScope_LockedSetting_DeniesToTenantAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _settingsResolver.ResolveWithMetadataAsync("deployment.mode", Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedSetting { Key = "deployment.mode", IsLocked = true });

        var result = await _service.CheckSettingAccessAsync("deployment.mode", "update", tenantId: TestTenantId);

        await Assert.That(result).IsFalse();
    }

    [Test]
    [Category("Phase43Ticketing")]
    public async Task IsAllowed_ManageTickets_RequiresVerifiedOrganizerOrExactEventPermission()
    {
        var eventId = Guid.NewGuid();
        var organizerUserId = Guid.NewGuid();
        _adminContext.UserId.Returns(organizerUserId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            CreateVerifiedOrganizerAttributes(eventId, userId: organizerUserId))).IsTrue();

        var managerUserId = Guid.NewGuid();
        _adminContext.UserId.Returns(managerUserId);
        ConfigureEventAuthority(managerUserId, eventId, PermissionCodes.EventManageTickets);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            CreateEventContextAttributes(eventId))).IsTrue();

        var updateOnlyUserId = Guid.NewGuid();
        _adminContext.UserId.Returns(updateOnlyUserId);
        ConfigureEventAuthority(updateOnlyUserId, eventId, PermissionCodes.EventUpdate);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            CreateEventContextAttributes(eventId))).IsFalse();

        var contributorUserId = Guid.NewGuid();
        _adminContext.UserId.Returns(contributorUserId);
        ConfigureEventAuthority(contributorUserId, eventId);
        var contributor = CreateEventContextAttributes(eventId);
        contributor["actorId"] = Guid.NewGuid();
        contributor["userId"] = contributorUserId;
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            contributor)).IsFalse();

        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            CreateEventContextAttributes(eventId))).IsFalse();

        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            eventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            CreateEventContextAttributes(eventId))).IsFalse();
    }

    [Test]
    [Category("Phase43Ticketing")]
    public async Task IsAllowed_ManageTickets_DeniesMissingAndCrossEventContext()
    {
        var userId = Guid.NewGuid();
        var authorizedEventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        ConfigureEventAuthority(userId, authorizedEventId, PermissionCodes.EventManageTickets);

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            authorizedEventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            new Dictionary<string, object> { ["eventId"] = authorizedEventId })).IsFalse();

        await Assert.That(await _service.IsAllowedAsync(
            ResourceKinds.Event,
            authorizedEventId.ToString(),
            AuthorizationActions.Events.ManageTickets,
            CreateEventContextAttributes(Guid.NewGuid()))).IsFalse();
    }

    [Test]
    [Category("Phase43Ticketing")]
    public async Task IsAllowedBatch_ManageTickets_MatchesParentEventAuthorityBoundaries()
    {
        var userId = Guid.NewGuid();
        var organizerEventId = Guid.NewGuid();
        var assignedEventId = Guid.NewGuid();
        var contributorEventId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminGroupIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(
                userId,
                PermissionCodes.EventCreate,
                Arg.Any<CancellationToken>())
            .Returns([]);
        ConfigureEventAuthority(userId, assignedEventId, PermissionCodes.EventManageTickets);

        var contributorAttributes = CreateEventContextAttributes(contributorEventId);
        contributorAttributes["actorId"] = Guid.NewGuid();
        contributorAttributes["userId"] = userId;

        var results = await _service.IsAllowedBatchAsync(
        [
            new(ResourceKinds.Event, organizerEventId.ToString(), AuthorizationActions.Events.ManageTickets, CreateVerifiedOrganizerAttributes(organizerEventId, userId: userId)),
            new(ResourceKinds.Event, assignedEventId.ToString(), AuthorizationActions.Events.ManageTickets, CreateEventContextAttributes(assignedEventId)),
            new(ResourceKinds.Event, contributorEventId.ToString(), AuthorizationActions.Events.ManageTickets, contributorAttributes),
            new(ResourceKinds.Event, assignedEventId.ToString(), AuthorizationActions.Events.ManageTickets, new Dictionary<string, object> { ["eventId"] = assignedEventId }),
            new(ResourceKinds.Event, assignedEventId.ToString(), AuthorizationActions.Events.ManageTickets, CreateEventContextAttributes(Guid.NewGuid()))
        ]);

        await Assert.That(results).IsEquivalentTo([true, true, false, false, false]);
    }

    private static Dictionary<string, object> CreateEventContextAttributes() =>
        CreateEventContextAttributes(Guid.NewGuid());

    private static Dictionary<string, object> CreateEventContextAttributes(Guid eventId) => new()
    {
        ["tenantId"] = TestTenantId,
        ["eventId"] = eventId
    };

    private static Dictionary<string, object> CreateVerifiedOrganizerAttributes(
        Guid eventId,
        Guid? userId = null,
        Guid? organizationId = null,
        Guid? groupId = null)
    {
        var attributes = CreateEventContextAttributes(eventId);
        attributes["actorId"] = Guid.NewGuid();
        attributes["organizerActorId"] = Guid.NewGuid();

        if (userId.HasValue)
            attributes["organizerUserId"] = userId.Value;
        if (organizationId.HasValue)
            attributes["organizerOrganizationId"] = organizationId.Value;
        if (groupId.HasValue)
            attributes["organizerGroupId"] = groupId.Value;

        return attributes;
    }

    private void ConfigureEventAuthority(Guid userId, Guid eventId, params string[] permissionCodes)
    {
        _eventAuthoritySnapshotService.GetForUserAndEventsAsync(
                TestTenantId,
                userId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var requestedEventIds = callInfo.ArgAt<IReadOnlyCollection<Guid>>(2);
                var events = requestedEventIds.ToDictionary(
                    requestedEventId => requestedEventId,
                    requestedEventId => requestedEventId == eventId
                        ? new EventAuthorityForUser(
                            new HashSet<string>(),
                            permissionCodes.ToHashSet(StringComparer.Ordinal),
                            IsOwner: false,
                            IsManager: permissionCodes.Contains(PermissionCodes.EventManageTeam, StringComparer.Ordinal))
                        : new EventAuthorityForUser(
                            new HashSet<string>(),
                            new HashSet<string>(),
                            IsOwner: false,
                            IsManager: false));

                return Task.FromResult(new EventAuthoritySnapshot(TestTenantId, userId, events));
            });
    }
}
