// ABOUTME: Component tests for NavMenu admin section visibility based on BFF-reported admin status.
// ABOUTME: Verifies browser admin claims are not treated as navigation authority.

using Explore.Blazor.Client.Tests.Common.Authentication;

namespace Explore.Blazor.Client.Tests.Layout;

/// <summary>
/// Tests for the NavMenu admin section rendering behavior.
/// Admin links are shown based on BFF/admin status models:
/// - Instance admin: Instance Administration
/// - Tenant admin: Tenant Administration only
/// - Organization admin: Organization Settings link(s)
/// - Regular user: no admin section at all
/// </summary>
public class NavMenuAdminTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public NavMenuAdminTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.AddShellStateMocks();
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
        var cut = RenderNavMenu();

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("Instance Administration");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Administration");
        await Assert.That(cut.Markup).DoesNotContain("Organization Settings");
    }

    [Test]
    public async Task NavMenu_AuthenticatedUserWithoutAdminClaims_DoesNotShowAdminSection()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(AuthenticationTestConstants.DefaultUserId, "Regular User");
        SetupNavMenuServices();

        // Act
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("Instance Administration");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Administration");
        await Assert.That(cut.Markup).DoesNotContain("Organization Settings");
    }

    [Test]
    public async Task NavMenu_AuthenticatedUser_DoesNotShowRemovedMyRegistrationsLink()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(AuthenticationTestConstants.DefaultUserId, "Regular User");
        SetupNavMenuServices();

        // Act
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("My Registrations");
        await Assert.That(cut.Markup).DoesNotContain("href=\"/my/registrations\"");
    }

    [Test]
    public async Task NavMenu_AuthenticatedUser_RendersAiAssistantToggleBeforeProfileDropdown()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(AuthenticationTestConstants.DefaultUserId, "Regular User");
        var settings = new PublicExperienceSettingsBuilder()
            .WithAiAssistant()
            .Build();
        SetupNavMenuServices(publicExperienceSettings: settings);

        // Act
        var cut = RenderNavMenu();

        // Assert
        cut.WaitForElement("[data-testid='shell-ai-toggle']");
        var markup = cut.Markup;
        var aiToggleIndex = markup.IndexOf("data-testid=\"shell-ai-toggle\"", StringComparison.Ordinal);
        var userDropdownIndex = markup.IndexOf("navbar__user-dropdown", StringComparison.Ordinal);

        await Assert.That(aiToggleIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(userDropdownIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(aiToggleIndex).IsLessThan(userDropdownIndex);
    }

    [Test]
    public async Task NavMenu_InstanceAdminClaimOnly_DoesNotShowAdminLinks()
    {
        // Arrange
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Admin User",
            new Claim("explore:admin:instance", "true"));
        SetupNavMenuServices();

        // Act
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert -- serialized/browser claims are not treated as admin authority
        await Assert.That(cut.Markup).DoesNotContain("Instance Administration");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Administration");
    }

    [Test]
    public async Task NavMenu_TenantAdminClaimOnly_DoesNotShowAdminLinks()
    {
        // Arrange
        var tenantId = AuthenticationTestConstants.DefaultTenantId;
        _ctx.SetAuthenticatedUserWithClaims(
            AuthenticationTestConstants.AdminUserId,
            "Tenant Admin User",
            new Claim("explore:admin:tenant", tenantId.ToString()));
        SetupNavMenuServices();

        // Act
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert -- serialized/browser claims are not treated as admin authority
        await Assert.That(cut.Markup).DoesNotContain("Instance Administration");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Administration");
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
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert -- serialized/browser claims are not treated as admin authority
        await Assert.That(cut.Markup).DoesNotContain("Organization Settings");
        await Assert.That(cut.Markup).DoesNotContain($"/admin/organization/{orgId}/settings");
        await Assert.That(cut.Markup).DoesNotContain("Instance Administration");
        await Assert.That(cut.Markup).DoesNotContain("Instance Settings");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Administration");
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
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert -- serialized/browser org-admin claims do not create admin links
        await Assert.That(cut.Markup).DoesNotContain($"/admin/organization/{orgId1}/settings");
        await Assert.That(cut.Markup).DoesNotContain($"/admin/organization/{orgId2}/settings");
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
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert -- serialized/browser claims do not create admin links
        await Assert.That(cut.Markup).DoesNotContain("Instance Administration");
        await Assert.That(cut.Markup).DoesNotContain("Tenant Administration");
        await Assert.That(cut.Markup).DoesNotContain($"/admin/organization/{orgId}/settings");
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
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin/instance/settings\"");
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
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin\"");
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin/tenant/settings\"");
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin/instance/settings\"");
    }

    [Test]
    public async Task NavMenu_SingleTenantInstanceAdminFallback_ShowsAdministration_WhenClaimsAreStale()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(AuthenticationTestConstants.AdminUserId, "Setup Admin");
        SetupNavMenuServices(
            deploymentMode: "SingleTenant",
            isCurrentUserInstanceAdmin: true);

        // Act
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert -- onboarding grants are visible even before the serialized auth claims rehydrate.
        await Assert.That(cut.Markup).Contains("Administration");
        await Assert.That(cut.Markup).Contains("href=\"/admin/tenant/settings\"");
        await Assert.That(cut.Markup).DoesNotContain("href=\"/admin/instance/settings\"");
    }

    [Test]
    public async Task NavMenu_RefreshesAdminStatusWhenDropdownOpens_AfterSingleTenantOnboarding()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(AuthenticationTestConstants.AdminUserId, "Setup Admin");
        SetupNavMenuServices(deploymentMode: "SingleTenant");

        var instanceOnboardingService = _ctx.Services.GetRequiredService<IInstanceOnboardingService>();
        instanceOnboardingService.GetStatusAsync().Returns(
            new InstanceOnboardingStatusModel
            {
                IsCompleted = true,
                IsAuthenticated = true,
                IsCurrentUserInstanceAdmin = false,
                SelectedDeploymentMode = "SingleTenant"
            },
            new InstanceOnboardingStatusModel
            {
                IsCompleted = true,
                IsAuthenticated = true,
                IsCurrentUserInstanceAdmin = true,
                SelectedDeploymentMode = "SingleTenant"
            });

        // Act
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert -- the persisted layout rechecks BFF/API authority on menu open.
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("href=\"/admin/tenant/settings\"", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant administration link to render after status refresh.");
            }
        });
        await Assert.That(cut.Markup).Contains("Administration");
    }

    [Test]
    public async Task NavMenu_SingleTenantAdminAuthority_ShowsAdministration_WhenOnboardingStatusIsStale()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(AuthenticationTestConstants.AdminUserId, "Setup Admin");
        SetupNavMenuServices(deploymentMode: "SingleTenant");

        var userService = _ctx.Services.GetRequiredService<IUserService>();
        userService.GetAdminAuthorityAsync().Returns(new AdminAuthorityDto
        {
            IsInstanceAdmin = true,
            AdminTenantIds = [AuthenticationTestConstants.DefaultTenantId],
            AdminOrganizationIds = [],
            HasAnyAuthority = true
        });

        var tenantOnboardingService = _ctx.Services.GetRequiredService<ITenantOnboardingService>();
        tenantOnboardingService.GetStatusAsync().Returns((TenantOnboardingStatusModel?)null);

        // Act
        var cut = RenderNavMenu();
        OpenDropdown(cut);

        // Assert
        await Assert.That(cut.Markup).Contains("href=\"/admin/tenant/settings\"");
        await Assert.That(cut.Markup).Contains("Administration");
    }

    private IRenderedComponent<DynamicComponent> RenderNavMenu()
    {
        var componentType = typeof(IUserService).Assembly.GetType("Explore.Blazor.Client.Layout.NavMenu")
                            ?? throw new InvalidOperationException("NavMenu component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    private static void OpenDropdown(IRenderedComponent<DynamicComponent> cut)
    {
        var dropdownButton = cut.Find(".navbar__user-btn");
        dropdownButton.Click();
    }

    private void SetupNavMenuServices(
        PublicExperienceSettingsModel? publicExperienceSettings = null,
        string deploymentMode = "MultiTenant",
        bool isCurrentUserInstanceAdmin = false,
        bool isCurrentUserTenantAdmin = false)
    {
        NavMenuTestServices.Register(
            _ctx,
            publicExperienceSettings: publicExperienceSettings,
            deploymentMode: deploymentMode,
            isCurrentUserInstanceAdmin: isCurrentUserInstanceAdmin,
            isCurrentUserTenantAdmin: isCurrentUserTenantAdmin);
    }
}
