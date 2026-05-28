// ABOUTME: Unit tests for FallbackAuthorizationService verifying DB-driven authorization logic.
// Tests the Instance > Tenant > Organization hierarchy and lock semantics.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Settings;
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
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FallbackAuthorizationService> _logger;
    private readonly FallbackAuthorizationService _service;

    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public FallbackAuthorizationServiceTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _machinePrincipalAccessor = Substitute.For<IMachinePrincipalAccessor>();
        _eventAuthoritySnapshotService = Substitute.For<IEventAuthoritySnapshotService>();
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
            _settingsResolver,
            _tenantContext,
            _logger);
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
    public async Task IsAllowed_NonAdmin_DeniesTenantUserRoleGrantCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("islamuevent_tenant_user_role_grant", Guid.NewGuid().ToString(), "create");

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
    public async Task IsAllowed_EventChildWithEventContext_AllowsTenantAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = CreateEventContextAttributes();

        var result = await _service.IsAllowedAsync("islamuevent_event_session", Guid.NewGuid().ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
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

    private static Dictionary<string, object> CreateEventContextAttributes() =>
        CreateEventContextAttributes(Guid.NewGuid());

    private static Dictionary<string, object> CreateEventContextAttributes(Guid eventId) => new()
    {
        ["tenantId"] = TestTenantId,
        ["eventId"] = eventId
    };

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
