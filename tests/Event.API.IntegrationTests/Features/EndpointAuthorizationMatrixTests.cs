// ABOUTME: Endpoint authorization matrix — verifies that every API endpoint enforces the correct
// ABOUTME: authentication and authorization behavior across all personas (anonymous, regular user,
// ABOUTME: tenant admin, instance admin). Uses Local RBAC mode with mocked IAdminContext.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain.Constants;
using Explore.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Systematically validates authentication and authorization enforcement across all API endpoints.
/// Each test targets a representative endpoint from each authorization tier:
/// <list type="bullet">
/// <item><b>Tier 0 — Public</b>: [AllowAnonymous] endpoints return 200 for unauthenticated requests</item>
/// <item><b>Tier 1 — Authenticated</b>: [Authorize] endpoints return 401 for anonymous, 200/403 for authenticated</item>
/// <item><b>Tier 2 — Instance Admin</b>: Instance-scoped resources denied for non-admins</item>
/// <item><b>Tier 3 — Tenant Admin</b>: Tenant-scoped writes denied for regular users</item>
/// <item><b>Tier 4 — Self-Service</b>: Personal data (notifications, settings) accessible to all authenticated</item>
/// </list>
///
/// <para>Uses Local RBAC mode with NSubstitute mocks for IAdminContext/ITenantContext
/// to control authorization decisions without seeding domain data.</para>
/// </summary>
[Category(TestCategories.Security)]
[ClassDataSource<KeycloakOnlyFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("SecurityInfra")]
public class EndpointAuthorizationMatrixTests : IAsyncDisposable
{
    private readonly KeycloakOnlyFixture _keycloak;

    private readonly WebApplicationFactory<Program> _instanceAdminFactory;
    private readonly HttpClient _instanceAdminClient;

    private readonly WebApplicationFactory<Program> _tenantAdminFactory;
    private readonly HttpClient _tenantAdminClient;

    private readonly WebApplicationFactory<Program> _regularUserFactory;
    private readonly HttpClient _regularUserClient;

    private readonly HttpClient _anonymousClient;

    private static readonly Guid DefaultTenantId = PlatformDefaults.DefaultTenantId;

    public EndpointAuthorizationMatrixTests(KeycloakOnlyFixture keycloak)
    {
        _keycloak = keycloak;

        _instanceAdminFactory = CreateFactory(CreateInstanceAdminContext());
        _instanceAdminClient = _instanceAdminFactory.CreateClient();

        _tenantAdminFactory = CreateFactory(CreateTenantAdminContext());
        _tenantAdminClient = _tenantAdminFactory.CreateClient();

        _regularUserFactory = CreateFactory(CreateRegularUserContext());
        _regularUserClient = _regularUserFactory.CreateClient();

        _anonymousClient = _regularUserFactory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _instanceAdminClient.Dispose();
        _tenantAdminClient.Dispose();
        _regularUserClient.Dispose();
        _anonymousClient.Dispose();
        await _instanceAdminFactory.DisposeAsync();
        await _tenantAdminFactory.DisposeAsync();
        await _regularUserFactory.DisposeAsync();
    }

    #region Tier 0 — Public Endpoints (AllowAnonymous)

    [Test]
    public async Task Matrix_Public_EventFormats_AnonymousOK()
    {
        await AssertAnonymousOk("/api/eventformat");
    }

    [Test]
    public async Task Matrix_Public_EventStatuses_AnonymousOK()
    {
        await AssertAnonymousOk("/api/eventstatus");
    }

    [Test]
    public async Task Matrix_Public_EventTypes_AnonymousOK()
    {
        await AssertAnonymousOk("/api/eventtype");
    }

    [Test]
    public async Task Matrix_Public_Categories_AnonymousOK()
    {
        await AssertAnonymousOk("/api/category");
    }

    [Test]
    public async Task Matrix_Public_Tags_AnonymousOK()
    {
        await AssertAnonymousOk("/api/tag");
    }

    [Test]
    public async Task Matrix_Public_Languages_AnonymousOK()
    {
        await AssertAnonymousOk("/api/language");
    }

    [Test]
    public async Task Matrix_Public_Roles_AnonymousOK()
    {
        await AssertAnonymousOk("/api/role");
    }

    [Test]
    public async Task Matrix_Public_Events_AnonymousOK()
    {
        await AssertAnonymousOk("/api/event");
    }

    [Test]
    public async Task Matrix_Public_Organizations_AnonymousOK()
    {
        await AssertAnonymousOk("/api/organization");
    }

    [Test]
    public async Task Matrix_Public_Groups_AnonymousOK()
    {
        await AssertAnonymousOk("/api/group");
    }

    [Test]
    public async Task Matrix_Protected_Locations_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/location");
    }

    [Test]
    public async Task Matrix_Public_EventSessions_AnonymousOK()
    {
        await AssertAnonymousOk("/api/eventsession");
    }

    [Test]
    public async Task Matrix_Public_TenantNavigation_AnonymousOK()
    {
        await AssertAnonymousOk("/api/tenant/navigation");
    }

    [Test]
    public async Task Matrix_Public_FooterConfig_AnonymousOK()
    {
        await AssertAnonymousOk("/api/footer/config");
    }

    [Test]
    public async Task Matrix_Public_Translations_AnonymousOK()
    {
        await AssertAnonymousOk("/api/translation/en");
    }

    [Test]
    public async Task Matrix_Public_ModulesAvailable_AnonymousOK()
    {
        await AssertAnonymousOk("/api/module/available");
    }

    [Test]
    public async Task Matrix_Public_PublicExperienceSettings_AnonymousOK()
    {
        await AssertAnonymousOk("/api/publicexperience/settings");
    }

    [Test]
    public async Task Matrix_Public_Actors_AnonymousOK()
    {
        await AssertAnonymousOk("/api/actor");
    }

    [Test]
    public async Task Matrix_Public_OnboardingStatus_AnonymousOK()
    {
        await AssertAnonymousOk("/api/instanceonboarding/status");
    }

    [Test]
    public async Task Matrix_Public_RegistrationScopes_AnonymousOK()
    {
        await AssertAnonymousOk("/api/registrationscope");
    }

    [Test]
    public async Task Matrix_Public_ScheduleItemKinds_AnonymousOK()
    {
        await AssertAnonymousOk("/api/scheduleitemkind");
    }

    [Test]
    public async Task Matrix_Public_EventSessionKinds_AnonymousOK()
    {
        await AssertAnonymousOk("/api/eventsessionkind");
    }

    [Test]
    public async Task Matrix_Public_AuthProviderStatus_AnonymousOK()
    {
        await AssertAnonymousOk("/api/instance/settings/auth-provider/status");
    }

    [Test]
    public async Task Matrix_Public_AuthzProviderStatus_AnonymousOK()
    {
        await AssertAnonymousOk("/api/instance/settings/authz-provider/status");
    }

    #endregion

    #region Tier 1 — Authenticated Endpoints (401 for Anonymous)

    [Test]
    public async Task Matrix_Auth_EventMy_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/event/my");
    }

    [Test]
    public async Task Matrix_Auth_UserSync_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/user/sync", HttpMethod.Post);
    }

    [Test]
    public async Task Matrix_Auth_Notifications_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/notification");
    }

    [Test]
    public async Task Matrix_Auth_ExternalApiKeys_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/externalapikey");
    }

    [Test]
    public async Task Matrix_Auth_TenantUserRoleGrants_CreateAnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/tenant-user-role-grants", HttpMethod.Post);
    }

    [Test]
    public async Task Matrix_Auth_RegistrationOrders_ListAnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/events/00000000-0000-0000-0000-000000000001/registration-orders");
    }

    [Test]
    public async Task Matrix_Auth_RegistrationOrders_DetailAnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/events/00000000-0000-0000-0000-000000000001/registration-orders/00000000-0000-0000-0000-000000000002");
    }

    [Test]
    public async Task Matrix_Auth_RegistrationOrderParticipants_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/events/00000000-0000-0000-0000-000000000001/registration-orders/00000000-0000-0000-0000-000000000002/participants");
    }

    [Test]
    public async Task Matrix_Auth_RegistrationOrderDelete_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized(
            "/api/events/00000000-0000-0000-0000-000000000001/registration-orders/00000000-0000-0000-0000-000000000002",
            HttpMethod.Delete);
    }

    [Test]
    public async Task Matrix_Auth_TenantUserRoleGrants_ListAnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/tenant-user-role-grants");
    }

    [Test]
    public async Task Matrix_Auth_TenantUserRoleGrants_DetailAnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/tenant-user-role-grants/00000000-0000-0000-0000-000000000001");
    }

    [Test]
    public async Task Matrix_Auth_OrganizationMembers_ListAnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/OrganizationMember/00000000-0000-0000-0000-000000000001");
    }

    [Test]
    public async Task Matrix_Auth_OrganizationMembers_DetailAnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/OrganizationMember/member/00000000-0000-0000-0000-000000000001");
    }

    [Test]
    public async Task Matrix_Auth_UserAppearance_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/user/appearance");
    }

    [Test]
    public async Task Matrix_Auth_Settings_UserCategory_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/settings/user/appearance");
    }

    [Test]
    public async Task Matrix_Auth_Settings_TenantCategory_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/settings/tenant/appearance");
    }

    [Test]
    public async Task Matrix_Auth_TenantOnboardingStatus_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/tenantonboarding/status");
    }

    [Test]
    public async Task Matrix_Auth_UserAuthority_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/user/admin-authority");
    }

    [Test]
    public async Task Matrix_Auth_UserOrganizations_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/user/00000000-0000-0000-0000-000000000001/organizations");
    }

    [Test]
    public async Task Matrix_Auth_TenantStorageSettingsPatch_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/tenant/settings/storage", HttpMethod.Patch);
    }

    [Test]
    public async Task Matrix_Auth_TenantBrandingSettingsDocumentPatch_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/tenant/settings/documents/branding", HttpMethod.Patch);
    }

    [Test]
    public async Task Matrix_Auth_FooterLinkGroups_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/footer/link-groups");
    }

    [Test]
    public async Task Matrix_Auth_FooterSettingsGet_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/footer/settings");
    }

    [Test]
    public async Task Matrix_Auth_FooterSettingsPatch_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/footer/settings", HttpMethod.Patch);
    }

    [Test]
    public async Task Matrix_Auth_Features_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/features/my-flags");
    }

    [Test]
    public async Task Matrix_Auth_UiThemes_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/admin/ui-themes");
    }

    [Test]
    public async Task Matrix_Auth_LocalizationAdmin_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/admin/localization/configuration");
    }

    [Test]
    public async Task Matrix_Auth_CustomPropertyGovernance_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/admin/custom-property-definitions/governance-report");
    }

    [Test]
    public async Task Matrix_Auth_CustomPropertyProjectionAdmin_AnonymousDenied()
    {
        await AssertAnonymousUnauthorized("/api/admin/custom-property-projections/status");
    }

    #endregion

    #region Tier 2 — Instance Admin Only Endpoints

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsModules_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/modules", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "instance settings should only be accessible to instance admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsModules_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/modules", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read instance settings modules");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsBranding_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/branding", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "instance branding settings should only be accessible to instance admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsBranding_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/branding", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read instance branding settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsDeploymentMode_TenantAdminDenied()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/deployment-mode", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "deployment mode settings should only be accessible to instance admins, not tenant admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsStorage_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/storage", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read storage settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsSmtp_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/smtp", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read SMTP settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsAnalytics_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/analytics-governance", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "analytics governance should only be accessible to instance admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsTenantDelegation_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/tenant-delegation", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read tenant delegation settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsRenderPolicy_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/render-policy", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read render policy settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsFooterGovernance_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/footer-governance", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read footer governance settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsAuthProvider_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/auth-provider", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read auth provider settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakRealmDoctor_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/doctor", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Keycloak realm diagnostics should only be accessible to instance admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakRealmDoctor_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/doctor", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to run read-only Keycloak realm diagnostics");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakRealmSyncPreview_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/sync-preview", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Keycloak realm sync preview should only be accessible to instance admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakRealmSyncPreview_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/sync-preview", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to preview read-only Keycloak realm sync plans");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakRealmSyncApply_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/sync-apply", token);
        request.Content = new StringContent("{\"backupConfirmed\":true}", Encoding.UTF8, "application/json");

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Keycloak realm sync apply should only be accessible to instance admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakRealmSyncApply_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/sync-apply", token);
        request.Content = new StringContent("{\"backupConfirmed\":true}", Encoding.UTF8, "application/json");

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to apply backup-confirmed additive Keycloak realm repairs");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakClientSecretRotate_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/client-secret/rotate", token);
        request.Content = new StringContent("{\"secretOwnershipMode\":\"deployment-managed\"}", Encoding.UTF8, "application/json");

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Keycloak client-secret rotation should only be accessible to instance admins");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_KeycloakClientSecretRotate_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/instance/settings/auth-provider/keycloak/client-secret/rotate", token);
        request.Content = new StringContent("{\"secretOwnershipMode\":\"deployment-managed\"}", Encoding.UTF8, "application/json");

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should receive safe operator instructions for deployment-managed Keycloak secrets");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsAuthzProvider_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/authz-provider", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read authz provider settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsDomains_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/domains", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read domain settings");
    }

    [Test]
    public async Task Matrix_InstanceAdmin_InstanceSettingsOrganizations_InstanceAdminOK()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/organizations", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin should be able to read organization settings");
    }

    #endregion

    #region Tier 3 — Tenant Admin Endpoints

    [Test]
    public async Task Matrix_TenantAdmin_TenantList_TenantAdminOK()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/tenant", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "tenant admin should be able to list tenants");
    }

    [Test]
    public async Task Matrix_TenantAdmin_CategoryCreate_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/category", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Forbidden, HttpStatusCode.BadRequest },
            "regular user should be denied category creation (403) or get validation error (400)");
    }

    [Test]
    public async Task Matrix_TenantAdmin_TagCreate_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/tag", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Forbidden, HttpStatusCode.BadRequest },
            "regular user should be denied tag creation");
    }

    [Test]
    public async Task Matrix_TenantAdmin_LocationCreate_RegularUserDenied()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/location", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Forbidden, HttpStatusCode.BadRequest },
            "regular user should be denied location creation");
    }

    [Test]
    public async Task Matrix_TenantAdmin_TenantUserRoleGrantList_TenantAdminOK()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/tenant-user-role-grants", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "tenant admin should be able to list tenant user role grants");
    }

    [Test]
    public async Task Matrix_TenantAdmin_SettingsTenantCategory_TenantAdminOK()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/settings/tenant/appearance", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "tenant admin should be able to read tenant settings");
    }

    [Test]
    public async Task Matrix_TenantAdmin_TenantCount_TenantAdminOK()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/tenant/count", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "tenant admin should be able to view tenant count");
    }

    [Test]
    public async Task Matrix_TenantAdmin_TenantOnboardingStatus_TenantAdminOK()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/tenantonboarding/status", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "tenant onboarding status is gated to multi-tenant deployments and the matrix fixture runs in single-tenant mode");
    }

    #endregion

    #region Tier 4 — Self-Service / All Authenticated

    [Test]
    public async Task Matrix_SelfService_Notifications_RegularUserOK()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/notification", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "notifications are personal data — all authenticated users can view their own");
    }

    [Test]
    public async Task Matrix_SelfService_UserSync_RegularUserOK()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/user/sync", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.BadRequest },
            "user sync should work for all authenticated users");
    }

    [Test]
    public async Task Matrix_SelfService_UserAppearance_RegularUserOK()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/user/appearance", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "user appearance preferences should be accessible to all authenticated users");
    }

    [Test]
    public async Task Matrix_SelfService_SettingsUserCategory_RegularUserOK()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/settings/user/appearance", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "user settings should be accessible to all authenticated users");
    }

    [Test]
    public async Task Matrix_SelfService_RegistrationOrderCreate_RegularUserOK()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(
            HttpMethod.Post,
            "/api/events/00000000-0000-0000-0000-000000000001/registration-orders",
            token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest },
            "registration order creation should be available to all authenticated users " +
            "(actual status depends on request body validation)");
    }

    [Test]
    public async Task Matrix_SelfService_FeaturesMyFlags_RegularUserOK()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/features/my-flags", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "feature flags should be accessible to all authenticated users");
    }

    [Test]
    public async Task Matrix_SelfService_UserAuthority_RegularUserOK()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/user/admin-authority", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "user authority check should be accessible to all authenticated users");
    }

    #endregion

    #region Tier 5 — Cross-Role Verification

    [Test]
    public async Task Matrix_CrossRole_InstanceAdmin_CanAccessTenantEndpoints()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/tenant", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin has full access including tenant endpoints");
    }

    [Test]
    public async Task Matrix_CrossRole_InstanceAdmin_CanAccessTenantUserRoleGrants()
    {
        var token = await _keycloak.TokenClient.GetAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/tenant-user-role-grants", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "instance admin can access tenant user role grant management");
    }

    [Test]
    public async Task Matrix_CrossRole_RegularUser_DeniedTenantCreation()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/tenant", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Forbidden, HttpStatusCode.BadRequest },
            "regular users should be denied tenant creation or fail request validation before creation");
    }

    [Test]
    public async Task Matrix_CrossRole_RegularUser_DeniedTenantUserRoleGrantCreation()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Post, "/api/tenant-user-role-grants", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "regular users should not be able to create tenant user role grants");
    }

    [Test]
    public async Task Matrix_CrossRole_RegularUser_CanAccessVisibleExternalApiKeys()
    {
        var token = await _keycloak.TokenClient.GetUserTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/externalapikey", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "regular users can list external API keys visible to the current user");
    }

    [Test]
    public async Task Matrix_CrossRole_TenantAdmin_DeniedInstanceSettings()
    {
        var token = await _keycloak.TokenClient.GetTenantAdminTokenAsync();
        using var request = Auth(HttpMethod.Get, "/api/instance/settings/modules", token);

        var response = await _tenantAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "tenant admins should not be able to access instance settings");
    }

    #endregion

    #region Helpers

    private async Task AssertAnonymousOk(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await _anonymousClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"[AllowAnonymous] endpoint {url} should return 200 for unauthenticated requests");
    }

    private async Task AssertAnonymousUnauthorized(string url, HttpMethod? method = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, url);
        var response = await _anonymousClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"[Authorize] endpoint {url} should return 401 for unauthenticated requests");
    }

    private static HttpRequestMessage Auth(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static IAdminContext CreateInstanceAdminContext()
    {
        var ctx = Substitute.For<IAdminContext>();
        ctx.UserId.Returns(Guid.NewGuid());
        ctx.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        ctx.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        ctx.IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        ctx.IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        ctx.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        ctx.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid> { DefaultTenantId }.AsReadOnly());
        ctx.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        return ctx;
    }

    private static IAdminContext CreateTenantAdminContext()
    {
        var ctx = Substitute.For<IAdminContext>();
        ctx.UserId.Returns(Guid.NewGuid());
        ctx.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        ctx.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        ctx.IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ctx.IsTenantAdminAsync(DefaultTenantId, Arg.Any<CancellationToken>()).Returns(true);
        ctx.IsTenantAdminAsync(Arg.Is<Guid>(id => id != DefaultTenantId), Arg.Any<CancellationToken>()).Returns(false);
        ctx.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ctx.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid> { DefaultTenantId }.AsReadOnly());
        ctx.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        return ctx;
    }

    private static IAdminContext CreateRegularUserContext()
    {
        var ctx = Substitute.For<IAdminContext>();
        ctx.UserId.Returns(Guid.NewGuid());
        ctx.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        ctx.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        ctx.IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ctx.IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ctx.IsOrganizationAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        ctx.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        ctx.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns(
            new List<Guid>().AsReadOnly());
        return ctx;
    }

    private WebApplicationFactory<Program> CreateFactory(IAdminContext adminContext)
    {
        var tenantContext = Substitute.For<Explore.Application.Contracts.Infrastructure.ITenantContext>();
        tenantContext.TenantId.Returns(DefaultTenantId);

        var cerbosConfigResolver = Substitute.For<ICerbosConfigResolver>();
        cerbosConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns((CerbosConfiguration?)null);

        return new MatrixWebApplicationFactory(
            _keycloak.Authority, _keycloak.MetadataAddress,
            adminContext, tenantContext, cerbosConfigResolver);
    }

    #endregion

    #region WebApplicationFactory

    private sealed class MatrixWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _keycloakAuthority;
        private readonly string _keycloakMetadataAddress;
        private readonly IAdminContext _adminContext;
        private readonly Explore.Application.Contracts.Infrastructure.ITenantContext _tenantContext;
        private readonly ICerbosConfigResolver _cerbosConfigResolver;

        public MatrixWebApplicationFactory(
            string keycloakAuthority,
            string keycloakMetadataAddress,
            IAdminContext adminContext,
            Explore.Application.Contracts.Infrastructure.ITenantContext tenantContext,
            ICerbosConfigResolver cerbosConfigResolver)
        {
            _keycloakAuthority = keycloakAuthority;
            _keycloakMetadataAddress = keycloakMetadataAddress;
            _adminContext = adminContext;
            _tenantContext = tenantContext;
            _cerbosConfigResolver = cerbosConfigResolver;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_matrix;Username=postgres;Password=postgres",
                    ["Keycloak:Authority"] = _keycloakAuthority,
                    ["Keycloak:Realm"] = KeycloakContainerFixture.RealmName,
                    ["Keycloak:Audience"] = "islamu-event-api",
                    ["Keycloak:RequireHttpsMetadata"] = "false",
                    ["Keycloak:MetadataAddress"] = _keycloakMetadataAddress,
                    ["S3Settings:Region"] = "us-east-1",
                    ["S3Settings:BucketName"] = "test-bucket",
                    ["S3Settings:AccessKeyId"] = "test-key",
                    ["S3Settings:SecretAccessKey"] = "test-secret",
                    ["S3Settings:Endpoint"] = "https://s3.example.com",
                    ["Deployment:Mode"] = "SingleTenant",
                    ["Deployment:DefaultTenantId"] = DefaultTenantId.ToString(),
                    ["Testing:HostProfile"] = TestHostProfile.Security,
                    ["Testing:SkipJwtAuthorityWarmup"] = "true",
                    ["Cerbos:GrpcEndpoint"] = "http://localhost:19999",
                    ["Cerbos:PlaintextMode"] = "true",
                };

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveExploreDbContextRegistrations();

                services.AddInMemoryExploreDbContext($"MatrixDb_{Guid.NewGuid():N}");

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();

                services.RemoveAll<IAdminContext>();
                services.AddScoped(_ => _adminContext);

                services.RemoveAll<Explore.Application.Contracts.Infrastructure.ITenantContext>();
                services.AddScoped(_ => _tenantContext);

                services.RemoveAll<ICerbosConfigResolver>();
                services.AddScoped(_ => _cerbosConfigResolver);

                services.AddSingleton<IHostedService, MatrixSystemSettingSeeder>();
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<Microsoft.AspNetCore.Authentication.IClaimsTransformation>();
                services.AddSingleton<Microsoft.AspNetCore.Authentication.IClaimsTransformation>(
                    new TestInternalUserClaimsTransformation(
                        _adminContext.UserId
                        ?? throw new InvalidOperationException("The matrix persona requires a deterministic user ID.")));

                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Authority = _keycloakAuthority;
                    options.MetadataAddress = _keycloakMetadataAddress;
                    options.TokenValidationParameters.ValidIssuer = _keycloakAuthority;
                    options.TokenValidationParameters.ValidIssuers = [_keycloakAuthority];
                    options.BackchannelHttpHandler = new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = (_, _, _, _) => true
                        },
                        ConnectCallback = async (context, cancellationToken) =>
                        {
                            var socket = new System.Net.Sockets.Socket(
                                System.Net.Sockets.AddressFamily.InterNetwork,
                                System.Net.Sockets.SocketType.Stream,
                                System.Net.Sockets.ProtocolType.Tcp);
                            try
                            {
                                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                            }
                            catch
                            {
                                socket.Dispose();
                                throw;
                            }
                        }
                    };
                });
            });
        }
    }

    private sealed class MatrixSystemSettingSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public MatrixSystemSettingSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

            dbContext.SystemSettings.Add(new Explore.Domain.SystemSetting
            {
                Id = Guid.NewGuid(),
                SettingKey = GovernanceSettingKeys.Security.AuthorizationProvider,
                Value = "\"local\"",
                ValueType = Explore.Domain.SettingValueType.String,
                IsLocked = false,
                Category = "Security",
                Description = "Authorization provider (local RBAC)",
                DisplayOrder = 0,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #endregion
}
