// ABOUTME: Data-driven Cerbos policy contract tests exercising all resource policies via the HTTP API.
// ABOUTME: Validates the 3-level admin hierarchy (instance > tenant > org) for every resource kind.

using Event.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Data-driven policy contract tests covering all Cerbos resource policies.
/// Uses the Cerbos HTTP API directly — not the API layer — to validate that
/// YAML policies produce correct ALLOW/DENY decisions for every role × resource × action.
/// </summary>
[Category(TestCategories.PolicyContract)]
[ClassDataSource<SecurityInfrastructureFixture>(Shared = SharedType.PerAssembly)]
public class CerbosPolicyContractTests : IDisposable
{
    private readonly CerbosTestClient _cerbos;

    public CerbosPolicyContractTests(SecurityInfrastructureFixture infra)
    {
        _cerbos = new CerbosTestClient(infra.CerbosHttpEndpoint);
    }

    public void Dispose()
    {
        _cerbos.Dispose();
    }

    #region Instance Admin — Full Access Across All Resources

    public static IEnumerable<(string ResourceKind, string[] Actions)> GetInstanceAdminFullAccessCases()
    {
        yield return ("event", (string[])["view", "create", "update", "delete"]);
        yield return ("organization", (string[])["view", "create", "update", "delete", "manage_members"]);
        yield return ("tenant", (string[])["view", "create", "update", "delete"]);
        yield return ("user", (string[])["view", "create", "update", "delete"]);
        yield return ("category", (string[])["view", "create", "update", "delete"]);
        yield return ("tag", (string[])["view", "create", "update", "delete"]);
        yield return ("location", (string[])["view", "create", "update", "delete"]);
        yield return ("location_room", (string[])["view", "create", "update", "delete"]);
        yield return ("actor", (string[])["view", "create", "update", "delete"]);
        yield return ("tenant_member", (string[])["view", "create", "update", "delete"]);
        yield return ("organization_member", (string[])["view", "create", "update", "delete", "manage_members"]);
        yield return ("group", (string[])["view", "create", "update", "delete"]);
        yield return ("group_member", (string[])["view", "create", "update", "delete"]);
        yield return ("storage_object", (string[])["view", "create", "update", "delete"]);
        yield return ("instance_setting", (string[])["view", "update", "delete", "lock", "unlock"]);
        yield return ("tenant_setting", (string[])["view", "update", "delete"]);
        yield return ("event_session", (string[])["view", "create", "update", "delete"]);
        yield return ("event_day", (string[])["view", "create", "update", "delete"]);
        yield return ("event_agenda_item", (string[])["view", "create", "update", "delete"]);
        yield return ("event_session_agenda_item", (string[])["view", "create", "update", "delete"]);
        yield return ("event_registration", (string[])["view", "create", "update", "delete"]);
        yield return ("organization_review", (string[])["view", "create", "update", "delete"]);
        yield return ("notification", (string[])["view", "create", "update", "delete"]);
        yield return ("custom_property_definition", (string[])["view", "create", "update", "delete"]);
        yield return ("custom_property_value", (string[])["view", "create", "update", "delete"]);
    }

    [Test]
    [MethodDataSource(nameof(GetInstanceAdminFullAccessCases))]
    public async Task InstanceAdmin_ShouldBeAllowed_AllActions((string ResourceKind, string[] Actions) testCase)
    {
        var (resourceKind, actions) = testCase;
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-instance-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = true, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: resourceKind,
            resourceId: "resource-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: actions);

        foreach (var action in actions)
        {
            result.Should().ContainKey(action).WhoseValue.Should().Be("EFFECT_ALLOW",
                $"instance admin should be allowed '{action}' on '{resourceKind}'");
        }
    }

    #endregion

    #region Tenant Admin — Scoped Access

    [Test]
    public async Task TenantAdmin_ShouldBeAllowed_ManageEventsInOwnTenant()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "event",
            resourceId: "event-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task TenantAdmin_ShouldBeDenied_ManageEventsInOtherTenant()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-other-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-other"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "event",
            resourceId: "event-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["create", "update", "delete"]);

        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY");
    }

    [Test]
    public async Task TenantAdmin_ShouldBeAllowed_ManageOrganizationsInOwnTenant()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "organization",
            resourceId: "org-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "update", "delete", "manage_members"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("manage_members").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task TenantAdmin_ShouldBeDenied_CreateOrganizationsInOtherTenant()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-other-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-other"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "organization",
            resourceId: "org-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["create", "update", "delete"]);

        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY");
    }

    [Test]
    public async Task TenantAdmin_ShouldBeAllowed_ViewAndUpdateOwnTenant()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "tenant",
            resourceId: "tenant-1",
            resourceAttrs: new { tenantId = "tenant-1" },
            actions: ["view", "update"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task TenantAdmin_ShouldBeDenied_ManageTenantSettings_WhenLockedByInstance()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "tenant_setting",
            resourceId: "setting-1",
            resourceAttrs: new { tenantId = "tenant-1", isLockedByInstance = true },
            actions: ["update"]);

        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY",
            "tenant admin must be denied update when instance admin has locked the setting");
    }

    [Test]
    public async Task TenantAdmin_ShouldBeAllowed_ManageTenantSettings_WhenNotLocked()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "tenant_setting",
            resourceId: "setting-1",
            resourceAttrs: new { tenantId = "tenant-1", isLockedByInstance = false },
            actions: ["view", "update"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    #endregion

    #region Org Admin — Scoped Access

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ManageEventsInOwnOrg()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "event",
            resourceId: "event-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeDenied_ManageEventsInOtherOrg()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-other-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-other"] = "admin" }
            },
            resourceKind: "event",
            resourceId: "event-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["create", "update", "delete"]);

        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ManageOwnOrg()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "organization",
            resourceId: "org-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "update", "manage_members"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("manage_members").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeDenied_DeleteOwnOrg()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "organization",
            resourceId: "org-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["delete"]);

        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY",
            "org admins can update/manage_members but not delete organizations");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ManageOrgMembers()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "organization_member",
            resourceId: "member-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete", "manage_members"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("manage_members").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ManageGroupsInOwnOrg()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "group",
            resourceId: "group-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ManageEventRegistrations()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "event_registration",
            resourceId: "reg-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "update", "delete"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ViewActors()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "actor",
            resourceId: "actor-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_DENY",
            "org admins can only view actors, not create them");
    }

    #endregion

    #region Regular User — View Only

    [Test]
    public async Task RegularUser_ShouldBeAllowed_ViewEvents()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "event",
            resourceId: "event-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task RegularUser_ShouldBeDenied_MutateEvents()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "event",
            resourceId: "event-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["create", "update", "delete"]);

        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY");
    }

    [Test]
    public async Task RegularUser_ShouldBeAllowed_RegisterForEvents()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "event_registration",
            resourceId: "reg-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["create", "view"]);

        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task RegularUser_ShouldBeAllowed_ViewAndUploadStorageObjects()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "storage_object",
            resourceId: "obj-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task RegularUser_ShouldBeDenied_DeleteStorageObjects()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "storage_object",
            resourceId: "obj-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["delete", "update"]);

        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY");
    }

    public static IEnumerable<string> GetAllViewableResources()
    {
        yield return "event";
        yield return "organization";
        yield return "tenant";
        yield return "user";
        yield return "category";
        yield return "tag";
        yield return "location";
        yield return "actor";
        yield return "tenant_member";
        yield return "organization_member";
        yield return "group";
        yield return "group_member";
        yield return "storage_object";
        yield return "instance_setting";
        yield return "event_session";
        yield return "event_day";
        yield return "event_agenda_item";
        yield return "event_session_agenda_item";
        yield return "event_registration";
        yield return "organization_review";
        yield return "notification";
        yield return "custom_property_definition";
        yield return "custom_property_value";
    }

    [Test]
    [MethodDataSource(nameof(GetAllViewableResources))]
    public async Task RegularUser_ShouldBeAllowed_ViewAllResources(string resourceKind)
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: resourceKind,
            resourceId: "resource-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW",
            $"regular users should be able to view '{resourceKind}' resources");
    }

    #endregion

    #region Instance Settings — Restricted to Instance Admin

    [Test]
    public async Task RegularUser_ShouldBeDenied_UpdateInstanceSettings()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "instance_setting",
            resourceId: "setting-1",
            resourceAttrs: new { tenantId = "tenant-1" },
            actions: ["update", "delete", "lock", "unlock"]);

        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("lock").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("unlock").WhoseValue.Should().Be("EFFECT_DENY");
    }

    [Test]
    public async Task TenantAdmin_ShouldBeDenied_ModifyInstanceSettings()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: "instance_setting",
            resourceId: "setting-1",
            resourceAttrs: new { tenantId = "tenant-1" },
            actions: ["update", "lock", "unlock"]);

        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY",
            "tenant admins cannot modify instance-level settings");
        result.Should().ContainKey("lock").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("unlock").WhoseValue.Should().Be("EFFECT_DENY");
    }

    #endregion

    #region Tenant-Scoped Lookup Resources

    public static IEnumerable<string> GetTenantScopedLookupResources()
    {
        yield return "category";
        yield return "tag";
        yield return "location";
    }

    [Test]
    [MethodDataSource(nameof(GetTenantScopedLookupResources))]
    public async Task TenantAdmin_ShouldBeAllowed_ManageLookupResourcesInOwnTenant(string resourceKind)
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new { }
            },
            resourceKind: resourceKind,
            resourceId: "resource-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        foreach (var action in new[] { "view", "create", "update", "delete" })
        {
            result.Should().ContainKey(action).WhoseValue.Should().Be("EFFECT_ALLOW",
                $"tenant admin should be allowed '{action}' on '{resourceKind}' in own tenant");
        }
    }

    [Test]
    [MethodDataSource(nameof(GetTenantScopedLookupResources))]
    public async Task RegularUser_ShouldBeDenied_MutateLookupResources(string resourceKind)
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: resourceKind,
            resourceId: "resource-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["create", "update", "delete"]);

        foreach (var action in new[] { "create", "update", "delete" })
        {
            result.Should().ContainKey(action).WhoseValue.Should().Be("EFFECT_DENY",
                $"regular user should be denied '{action}' on '{resourceKind}'");
        }
    }

    #endregion

    #region Org-Scoped Event Sub-Resources

    public static IEnumerable<string> GetOrgScopedEventSubResources()
    {
        yield return "event_session";
        yield return "event_day";
        yield return "event_agenda_item";
        yield return "event_session_agenda_item";
    }

    [Test]
    [MethodDataSource(nameof(GetOrgScopedEventSubResources))]
    public async Task OrgAdmin_ShouldBeAllowed_ManageEventSubResourcesInOwnOrg(string resourceKind)
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: resourceKind,
            resourceId: "sub-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        foreach (var action in new[] { "view", "create", "update", "delete" })
        {
            result.Should().ContainKey(action).WhoseValue.Should().Be("EFFECT_ALLOW",
                $"org admin should be allowed '{action}' on '{resourceKind}' in own org");
        }
    }

    [Test]
    [MethodDataSource(nameof(GetOrgScopedEventSubResources))]
    public async Task RegularUser_ShouldBeAllowed_ViewEventSubResources(string resourceKind)
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: resourceKind,
            resourceId: "sub-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_DENY",
            $"regular users should be denied create on '{resourceKind}'");
    }

    #endregion

    #region Organization Reviews — User Can Create

    [Test]
    public async Task RegularUser_ShouldBeAllowed_CreateOrganizationReviews()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "organization_review",
            resourceId: "review-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["create", "view"]);

        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_ALLOW",
            "all authenticated users can create organization reviews");
        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
    }

    [Test]
    public async Task RegularUser_ShouldBeDenied_DeleteOrganizationReviews()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "organization_review",
            resourceId: "review-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["delete", "update"]);

        result.Should().ContainKey("delete").WhoseValue.Should().Be("EFFECT_DENY");
        result.Should().ContainKey("update").WhoseValue.Should().Be("EFFECT_DENY");
    }

    #endregion

    #region Notification — All Users Can Manage

    [Test]
    public async Task RegularUser_ShouldBeAllowed_ManageNotifications()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "notification",
            resourceId: "notif-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        foreach (var action in new[] { "view", "create", "update", "delete" })
        {
            result.Should().ContainKey(action).WhoseValue.Should().Be("EFFECT_ALLOW",
                "all authenticated users should be able to manage their notifications");
        }
    }

    #endregion

    #region Custom Property Resources

    [Test]
    public async Task RegularUser_ShouldBeAllowed_ViewCustomPropertyDefinitions()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-regular",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = false, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "custom_property_definition",
            resourceId: "cpd-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW");
        result.Should().ContainKey("create").WhoseValue.Should().Be("EFFECT_DENY",
            "only tenant admins can create custom property definitions");
    }

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ManageCustomPropertyValues()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "custom_property_value",
            resourceId: "cpv-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        foreach (var action in new[] { "view", "create", "update", "delete" })
        {
            result.Should().ContainKey(action).WhoseValue.Should().Be("EFFECT_ALLOW",
                $"org admin should manage custom property values for their org");
        }
    }

    #endregion

    #region Location Room — Org Admin Access

    [Test]
    public async Task OrgAdmin_ShouldBeAllowed_ManageLocationRooms()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new { },
                orgMemberships = new Dictionary<string, string> { ["org-1"] = "admin" }
            },
            resourceKind: "location_room",
            resourceId: "room-1",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update", "delete"]);

        foreach (var action in new[] { "view", "create", "update", "delete" })
        {
            result.Should().ContainKey(action).WhoseValue.Should().Be("EFFECT_ALLOW",
                $"org admin should manage location rooms for their org");
        }
    }

    #endregion
}
