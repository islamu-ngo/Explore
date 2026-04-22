// ABOUTME: Unit tests for FallbackAuthorizationService verifying DB-driven authorization logic.
// Tests the Instance > Tenant > Organization hierarchy and lock semantics.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Settings;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Behaviors;

public class FallbackAuthorizationServiceTests
{
    private readonly IAdminContext _adminContext;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FallbackAuthorizationService> _logger;
    private readonly FallbackAuthorizationService _service;

    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public FallbackAuthorizationServiceTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _logger = Substitute.For<ILogger<FallbackAuthorizationService>>();

        _tenantContext.TenantId.Returns(TestTenantId);

        _service = new FallbackAuthorizationService(_adminContext, _settingsResolver, _tenantContext, _logger);
    }

    // === Instance Admin Tests ===

    [Test]
    public async Task IsAllowed_InstanceAdmin_AllowsEverything()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync("instance_setting", "any-key", "update");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_InstanceAdmin_AllowsTenantSettingEvenWhenLocked()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["isLockedByInstance"] = true };
        var result = await _service.IsAllowedAsync("tenant_setting", "locked-key", "update", attrs);

        await Assert.That(result).IsTrue();
    }

    // === Instance Setting Access ===

    [Test]
    public async Task IsAllowed_NonInstanceAdmin_DeniesInstanceSetting()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("instance_setting", "any-key", "update");

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

        var result = await _service.IsAllowedAsync("tenant_setting", "unlocked-key", "update", attrs);

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

        var result = await _service.IsAllowedAsync("tenant_setting", "locked-key", "update", attrs);

        await Assert.That(result).IsFalse();
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

        var result = await _service.IsAllowedAsync("tenant_setting", "some-key", "update", attrs);

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
        var result = await _service.IsAllowedAsync("organization", TestOrgId.ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsOrganizationInTheirTenant()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var result = await _service.IsAllowedAsync("organization", TestOrgId.ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonOrgAdmin_DeniesOrganizationResource()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(false);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var result = await _service.IsAllowedAsync("organization", TestOrgId.ToString(), "update", attrs);

        await Assert.That(result).IsFalse();
    }

    // === Tenant Member Access ===

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsTenantMemberCrud()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["tenantId"] = TestTenantId.ToString() };
        var result = await _service.IsAllowedAsync("tenant_member", Guid.NewGuid().ToString(), "create", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesTenantMemberCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("tenant_member", Guid.NewGuid().ToString(), "create");

        await Assert.That(result).IsFalse();
    }

    // === Group Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsGroupView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("group", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_OrgAdmin_AllowsGroupUpdate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(TestOrgId, Arg.Any<CancellationToken>()).Returns(true);

        var attrs = new Dictionary<string, object> { ["organizationId"] = TestOrgId };
        var result = await _service.IsAllowedAsync("group", Guid.NewGuid().ToString(), "update", attrs);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesGroupUpdate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("group", Guid.NewGuid().ToString(), "update");

        await Assert.That(result).IsFalse();
    }

    // === Group Member Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsGroupMemberViewAndCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var viewResult = await _service.IsAllowedAsync("group_member", Guid.NewGuid().ToString(), "view");
        var createResult = await _service.IsAllowedAsync("group_member", Guid.NewGuid().ToString(), "create");

        await Assert.That(viewResult).IsTrue();
        await Assert.That(createResult).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesGroupMemberDelete()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("group_member", Guid.NewGuid().ToString(), "delete");

        await Assert.That(result).IsFalse();
    }

    // === Custom Property Definition Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsCustomPropertyDefinitionView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("custom_property_definition", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_TenantAdmin_AllowsCustomPropertyDefinitionCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync("custom_property_definition", Guid.NewGuid().ToString(), "create");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesCustomPropertyDefinitionCreate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("custom_property_definition", Guid.NewGuid().ToString(), "create");

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
        var viewResult = await _service.IsAllowedAsync("event_contact_share_consent", Guid.NewGuid().ToString(), "viewsharedcontacts", attrs);
        var exportResult = await _service.IsAllowedAsync("event_contact_share_consent", Guid.NewGuid().ToString(), "exportsharedcontacts", attrs);

        await Assert.That(viewResult).IsTrue();
        await Assert.That(exportResult).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesContactShareConsent()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("event_contact_share_consent", Guid.NewGuid().ToString(), "viewsharedcontacts");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_ContactShareConsent_DeniesUnknownAction()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.IsAllowedAsync("event_contact_share_consent", Guid.NewGuid().ToString(), "delete");

        await Assert.That(result).IsFalse();
    }

    // === Notification Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsNotificationCrud()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        var viewResult = await _service.IsAllowedAsync("notification", Guid.NewGuid().ToString(), "view");
        var deleteResult = await _service.IsAllowedAsync("notification", Guid.NewGuid().ToString(), "delete");

        await Assert.That(viewResult).IsTrue();
        await Assert.That(deleteResult).IsTrue();
    }

    // === Actor Access ===

    [Test]
    public async Task IsAllowed_AuthenticatedUser_AllowsActorView()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("actor", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsAllowed_NonAdmin_DeniesActorUpdate()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.IsAllowedAsync("actor", Guid.NewGuid().ToString(), "update");

        await Assert.That(result).IsFalse();
    }

    // === SafeMode Tests ===

    [Test]
    public async Task IsAllowed_SafeMode_DeniesNonInstanceAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _service.ActivateSafeMode();

        var result = await _service.IsAllowedAsync("event", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsAllowed_SafeMode_AllowsInstanceAdmin()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _service.ActivateSafeMode();

        var result = await _service.IsAllowedAsync("event", Guid.NewGuid().ToString(), "view");

        await Assert.That(result).IsTrue();
    }

    // === Batch Optimization Tests ===

    [Test]
    public async Task IsAllowedBatch_ReturnsCorrectResults_ForMixedChecks()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        var checks = new List<AuthorizationCheck>
        {
            new("notification", Guid.NewGuid().ToString(), "view"),
            new("tenant_member", Guid.NewGuid().ToString(), "create"),
            new("instance_setting", "key", "update"),
            new("group", Guid.NewGuid().ToString(), "view"),
        };

        var results = await _service.IsAllowedBatchAsync(checks);

        await Assert.That(results).Count().IsEqualTo(4);
        await Assert.That(results[0]).IsTrue();  // notification: all authenticated
        await Assert.That(results[1]).IsTrue();  // tenant_member: tenant admin
        await Assert.That(results[2]).IsFalse(); // instance_setting: only instance admin
        await Assert.That(results[3]).IsTrue();  // group view: all authenticated
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
}
