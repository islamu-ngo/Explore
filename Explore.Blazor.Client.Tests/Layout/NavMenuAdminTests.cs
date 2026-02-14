// ABOUTME: Component tests for NavMenu admin section visibility based on DB-backed admin claims.
// Verifies that admin menu items are shown/hidden based on explore:admin:* claims.

using Explore.Blazor.Client.Layout;
using Explore.Blazor.Client.Tests.Common.Authentication;

namespace Explore.Blazor.Client.Tests.Layout;

/// <summary>
/// Tests for the NavMenu admin section rendering behavior.
/// The admin section (Admin Dashboard, Instance Settings, Tenant Settings) should only
/// appear when the authenticated user has explore:admin:instance or explore:admin:tenant claims.
/// The admin section is inside the user dropdown, so tests must open it before asserting.
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

        // Assert — dropdown is open but no admin section for regular users
        await Assert.That(cut.Markup).DoesNotContain("Admin Dashboard");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Settings");
    }

    [Test]
    public async Task NavMenu_InstanceAdmin_ShowsAdminSection()
    {
        // Arrange — user with explore:admin:instance claim
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Admin User",
            new Claim("explore:admin:instance", "true"));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).Contains("Admin Dashboard");
        await Assert.That(cut.Markup).Contains("Instance Settings");
        await Assert.That(cut.Markup).Contains("Tenant Settings");
    }

    [Test]
    public async Task NavMenu_TenantAdmin_ShowsAdminSection()
    {
        // Arrange — user with explore:admin:tenant claim
        var tenantId = AuthenticationTestConstants.DefaultTenantId;
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Admin User",
            new Claim("explore:admin:tenant", tenantId.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).Contains("Admin Dashboard");
        await Assert.That(cut.Markup).Contains("Instance Settings");
        await Assert.That(cut.Markup).Contains("Tenant Settings");
    }

    [Test]
    public async Task NavMenu_OrganizationAdminOnly_DoesNotShowAdminSection()
    {
        // Arrange — user with only explore:admin:organization claim (not instance/tenant)
        var orgId = Guid.NewGuid();
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Org Admin User",
            new Claim("explore:admin:organization", orgId.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = _ctx.RenderComponent<NavMenu>();
        OpenDropdown(cut);

        // Assert — organization admin alone does NOT show admin section
        await Assert.That(cut.Markup).DoesNotContain("Admin Dashboard");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Settings");
    }

    [Test]
    public async Task NavMenu_AdminSection_ContainsCorrectLinks()
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

        // Assert — verify the admin links point to correct routes
        await Assert.That(cut.Markup).Contains("href=\"/admin\"");
        await Assert.That(cut.Markup).Contains("href=\"/admin/instance/settings\"");
        await Assert.That(cut.Markup).Contains("href=\"/admin/tenant/settings\"");
    }

    /// <summary>
    /// Opens the user dropdown by clicking the toggle button.
    /// The admin section is inside the dropdown and only rendered when _dropdownOpen is true.
    /// </summary>
    private static void OpenDropdown(IRenderedComponent<NavMenu> cut)
    {
        var dropdownButton = cut.Find(".navbar__user-btn");
        dropdownButton.Click();
    }

    /// <summary>
    /// Registers all mock services required by NavMenu component.
    /// </summary>
    private void SetupNavMenuServices()
    {
        // IUserService — NavMenu calls GetCurrentUserAsync
        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns((UserDto?)null);
        _ctx.Services.AddSingleton(userService);

        // IPublicExperienceService — NavMenu calls GetSettingsAsync for branding
        var publicExperienceService = Substitute.For<IPublicExperienceService>();
        publicExperienceService.GetSettingsAsync().Returns((PublicExperienceSettingsModel?)null);
        _ctx.Services.AddSingleton(publicExperienceService);

        // ITenantNavigationService — NavMenu calls GetNavigationLinksAsync
        var tenantNavigationService = Substitute.For<ITenantNavigationService>();
        tenantNavigationService.GetNavigationLinksAsync().Returns(new List<TenantNavigationLinkDto>());
        _ctx.Services.AddSingleton(tenantNavigationService);
    }
}
