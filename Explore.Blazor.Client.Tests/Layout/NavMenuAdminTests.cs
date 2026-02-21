// ABOUTME: Component tests for NavMenu admin section visibility based on DB-backed admin claims.
// Verifies that admin menu items are shown/hidden per admin authority level (instance, tenant, organization).

using Explore.Blazor.Client.Layout;
using Explore.Blazor.Client.Tests.Common.Authentication;

namespace Explore.Blazor.Client.Tests.Layout;

/// <summary>
/// Tests for the NavMenu admin section rendering behavior.
/// Admin links are shown based on the user's admin authority claims:
/// - Instance admin: Admin Dashboard + Instance Settings
/// - Tenant admin: Tenant Settings only
/// - Organization admin: Organization Settings link(s)
/// - Regular user: no admin section at all
/// </summary>
public class NavMenuAdminTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public NavMenuAdminTests()
    {
        _ctx = new BlazorTestContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task NavMenu_AnonymousUser_DoesNotShowAdminSection()
    {
        // Arrange
        _ctx.SetAnonymousUser();
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("Admin Dashboard");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Settings");
        await Assert.That(cut.Markup).DoesNotContain("Organization Settings");
    }

    [Test]
    public async Task NavMenu_AuthenticatedUserWithoutAdminClaims_DoesNotShowAdminSection()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(AuthenticationTestConstants.DefaultUserId, "Regular User");
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("Admin Dashboard");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Settings");
        await Assert.That(cut.Markup).DoesNotContain("Organization Settings");
    }

    [Test]
    public async Task NavMenu_InstanceAdmin_ShowsAllPlatformAdminLinks()
    {
        // Arrange
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Admin User",
            new Claim("explore:admin:instance", "true"));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert -- instance admin sees Dashboard and Instance Settings only
        await Assert.That(cut.Markup).Contains("Admin Dashboard");
        await Assert.That(cut.Markup).Contains("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Settings");
    }

    [Test]
    public async Task NavMenu_TenantAdmin_ShowsTenantSettingsOnly()
    {
        // Arrange
        var tenantId = AuthenticationTestConstants.DefaultTenantId;
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Tenant Admin User",
            new Claim("explore:admin:tenant", tenantId.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert -- tenant admin sees Tenant Settings but not instance-level links
        await Assert.That(cut.Markup).DoesNotContain("Admin Dashboard");
        await Assert.That(cut.Markup).Contains("Tenant Settings");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
    }

    [Test]
    public async Task NavMenu_OrganizationAdminOnly_ShowsOrganizationSettings()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Org Admin User",
            new Claim("explore:admin:organization", orgId.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert -- org admin sees Organization Settings but not platform-level admin links
        await Assert.That(cut.Markup).Contains("Organization Settings");
        await Assert.That(cut.Markup).Contains($"/admin/organization/{orgId}/settings");
        await Assert.That(cut.Markup).DoesNotContain("Admin Dashboard");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Settings");
    }

    [Test]
    public async Task NavMenu_OrganizationAdmin_MultipleOrgs_ShowsLinkPerOrg()
    {
        // Arrange
        var orgId1 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Multi-Org Admin",
            new Claim("explore:admin:organization", orgId1.ToString()),
            new Claim("explore:admin:organization", orgId2.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert -- one link per administered organization
        await Assert.That(cut.Markup).Contains($"/admin/organization/{orgId1}/settings");
        await Assert.That(cut.Markup).Contains($"/admin/organization/{orgId2}/settings");
    }

    [Test]
    public async Task NavMenu_InstanceAdminWithOrgClaim_ShowsBothPlatformAndOrgLinks()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Super Admin",
            new Claim("explore:admin:instance", "true"),
            new Claim("explore:admin:organization", orgId.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert -- sees instance admin links plus the org link
        await Assert.That(cut.Markup).Contains("Admin Dashboard");
        await Assert.That(cut.Markup).Contains("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Settings");
        await Assert.That(cut.Markup).Contains($"/admin/organization/{orgId}/settings");
    }

    [Test]
    public async Task NavMenu_InstanceAdmin_ContainsCorrectRoutes()
    {
        // Arrange
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Admin User",
            new Claim("explore:admin:instance", "true"));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).Contains("href=\"/admin\"");
        await Assert.That(cut.Markup).Contains("href=\"/admin/instance/settings\"");
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin/tenant/settings\"");
    }

    [Test]
    public async Task NavMenu_TenantAdmin_ContainsCorrectRoutes()
    {
        // Arrange
        var tenantId = AuthenticationTestConstants.DefaultTenantId;
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Tenant Admin",
            new Claim("explore:admin:tenant", tenantId.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin\"");
        await Assert.That(cut.Markup).Contains("href=\"/admin/tenant/settings\"");
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin/instance/settings\"");
    }

    private static void OpenDropdown(IRenderedComponent<NavMenu> cut)
    {
        var dropdownButton = cut.Find(".navbar__user-btn");
        dropdownButton.Click();
    }

    private void SetupNavMenuServices()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns((UserDto?)null);
        _ctx.Services.AddSingleton(userService);

        var publicExperienceService = Substitute.For<IPublicExperienceService>();
        publicExperienceService.GetSettingsAsync().Returns((PublicExperienceSettingsModel?)null);
        _ctx.Services.AddSingleton(publicExperienceService);

        var tenantNavigationService = Substitute.For<ITenantNavigationService>();
        tenantNavigationService.GetNavigationLinksAsync().Returns(new List<TenantNavigationLinkDto>());
        _ctx.Services.AddSingleton(tenantNavigationService);

        var eligibilityService = Substitute.For<IEventCreationEligibilityService>();
        eligibilityService.GetEligibilityAsync().Returns(EventCreationEligibility.NotEligible);
        _ctx.Services.AddSingleton(eligibilityService);
    }
}
