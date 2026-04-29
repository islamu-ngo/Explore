// ABOUTME: Cerbos policy compilation and structural validation tests.
// ABOUTME: Verifies all policy files are loadable, the Cerbos container accepts them, and the health API confirms readiness.

using Event.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Validates that all Cerbos policy files compile and load correctly in the containerized PDP.
/// Uses the Cerbos HTTP API to verify policy loading, health status, and schema validation.
/// Catches YAML syntax errors, invalid rule conditions, missing imports, and schema mismatches
/// before they reach production.
/// </summary>
[Category(TestCategories.PolicyContract)]
[ClassDataSource<SecurityInfrastructureFixture>(Shared = SharedType.PerAssembly)]
public class CerbosPolicyCompilationTests : IDisposable
{
    private readonly CerbosTestClient _cerbos;

    public CerbosPolicyCompilationTests(SecurityInfrastructureFixture infra)
    {
        _cerbos = new CerbosTestClient(infra.CerbosHttpEndpoint);
    }

    public void Dispose()
    {
        _cerbos.Dispose();
    }

    #region Container Health

    [Test]
    public async Task CerbosContainer_ShouldBeHealthy()
    {
        var response = await _cerbos.HealthAsync();

        response.Should().BeSuccessful("the Cerbos container must report healthy after startup");
    }

    [Test]
    public async Task CerbosContainer_ShouldServeOpenAPISpec()
    {
        var response = await _cerbos.GetSchemaAsync();

        response.Should().BeSuccessful("the Cerbos HTTP API must be serving the schema endpoint");
    }

    #endregion

    #region Derived Roles Loading

    [Test]
    public async Task DerivedRoles_ShouldBeLoaded()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "test-derived-roles",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = true, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "event",
            resourceId: "test",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view"]);

        result.Should().ContainKey("view").WhoseValue.Should().Be("EFFECT_ALLOW",
            "the derived roles (explore_admin_roles) must be loaded for instance_admin to match");
    }

    #endregion

    #region All Resource Policies Are Loadable

    public static IEnumerable<string> GetAllExpectedResourcePolicies()
    {
        yield return "event";
        yield return "organization";
        yield return "tenant";
        yield return "user";
        yield return "category";
        yield return "tag";
        yield return "location";
        yield return "location_room";
        yield return "actor";
        yield return "tenant_member";
        yield return "organization_member";
        yield return "organization_review";
        yield return "group";
        yield return "group_member";
        yield return "storage_object";
        yield return "instance_setting";
        yield return "tenant_setting";
        yield return "event_session";
        yield return "event_day";
        yield return "event_agenda_item";
        yield return "event_session_agenda_item";
        yield return "event_registration";
        yield return "event_contact_share_consent";
        yield return "notification";
        yield return "custom_property_definition";
        yield return "custom_property_value";
        yield return "custom_property_governance";
        yield return "custom_property_projection";
        yield return "custom_property_template";
        yield return "indexed_did";
        yield return "atproto_record";
    }

    [Test]
    [MethodDataSource(nameof(GetAllExpectedResourcePolicies))]
    public async Task ResourcePolicy_ShouldBeLoadable_AndAcceptCheckRequests(string resourceKind)
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "compilation-test-user",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = true, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: resourceKind,
            resourceId: "compilation-test",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view"]);

        result.Should().ContainKey("view",
            $"resource policy '{resourceKind}' must be loaded and accept check requests");
        result["view"].Should().Be("EFFECT_ALLOW",
            $"instance admin should always be allowed to view '{resourceKind}'");
    }

    #endregion

    #region Policy Accepts Correct Attributes

    [Test]
    public async Task EventPolicy_ShouldAcceptTenantIdAttribute()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-42"] = "admin" },
                orgMemberships = new Dictionary<string, string>()
            },
            resourceKind: "event",
            resourceId: "evt-1",
            resourceAttrs: new { tenantId = "tenant-42", organizationId = "org-1" },
            actions: ["view", "create"]);

        result["view"].Should().Be("EFFECT_ALLOW");
        result["create"].Should().Be("EFFECT_ALLOW",
            "tenant admin matching tenantId must be allowed to create events");
    }

    [Test]
    public async Task OrgPolicy_ShouldAcceptOrganizationIdAttribute()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-org-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string>(),
                orgMemberships = new Dictionary<string, string> { ["org-99"] = "admin" }
            },
            resourceKind: "organization",
            resourceId: "org-99",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-99" },
            actions: ["view", "update"]);

        result["view"].Should().Be("EFFECT_ALLOW");
        result["update"].Should().Be("EFFECT_ALLOW",
            "org admin matching organizationId must be allowed to update their org");
    }

    [Test]
    public async Task TenantSettingPolicy_ShouldAcceptIsLockedByInstanceAttribute()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "user-tenant-admin",
            principalRoles: ["authenticated_user"],
            principalAttrs: new
            {
                isInstanceAdmin = false,
                tenantMemberships = new Dictionary<string, string> { ["tenant-1"] = "admin" },
                orgMemberships = new Dictionary<string, string>()
            },
            resourceKind: "tenant_setting",
            resourceId: "setting-1",
            resourceAttrs: new { tenantId = "tenant-1", isLockedByInstance = true },
            actions: ["update"]);

        result["update"].Should().Be("EFFECT_DENY",
            "tenant admin must be denied update when isLockedByInstance=true");
    }

    #endregion

    #region Negative — Unknown Resource Kind

    [Test]
    public async Task UnknownResourceKind_ShouldDenyAllActions()
    {
        var result = await _cerbos.CheckResourceAsync(
            principalId: "test-user",
            principalRoles: ["authenticated_user"],
            principalAttrs: new { isInstanceAdmin = true, tenantMemberships = new { }, orgMemberships = new { } },
            resourceKind: "nonexistent_resource_kind",
            resourceId: "test",
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create"]);

        result["view"].Should().Be("EFFECT_DENY",
            "unknown resource kinds must be denied — no matching policy");
        result["create"].Should().Be("EFFECT_DENY");
    }

    #endregion
}
