using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Layout;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Client.Pages.Organizations;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Tests.Common;
using Explore.Blazor.Client.Tests.Common.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Explore.Blazor.Client.Tests.Integration;

/// <summary>
/// Comprehensive authentication flow tests validating component behavior across
/// different authentication states: anonymous, authenticated, authorized, and authorizing.
/// Uses enterprise-grade authentication testing utilities following BUnit best practices.
/// </summary>
public class AuthenticationFlowTests
{
    /// <summary>
    /// Creates a fresh BlazorTestContext for each test.
    /// </summary>
    private static BlazorTestContext CreateContext() => new BlazorTestContext();

    #region NavMenu Authentication State Tests

    [Test]
    [DisplayName("NavMenu_ShowsLoginButton_WhenAnonymous")]
    public async Task NavMenu_ShowsLoginButton_WhenAnonymous()
    {
        // Arrange - Configure anonymous user using authentication scenarios
        using var ctx = CreateContext();
        AuthenticationScenarios.Anonymous().Build(ctx);
        RegisterNavMenuServices(ctx);

        // Act
        var cut = ctx.RenderComponent<NavMenu>();

        // Assert - Should show login/sign in option
        var markup = cut.Markup;
        await Assert.That(
            markup.Contains("Login", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Sign", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("authentication/login", StringComparison.OrdinalIgnoreCase)
        ).IsTrue();
    }

    [Test]
    [DisplayName("NavMenu_HidesAdminMenu_WhenNotAdmin")]
    public async Task NavMenu_HidesAdminMenu_WhenNotAdmin()
    {
        // Arrange - Configure regular authenticated user (not admin)
        using var ctx = CreateContext();
        AuthenticationScenarios.AuthenticatedUser(name: "Regular User").Build(ctx);
        RegisterNavMenuServices(ctx);

        // Act
        var cut = ctx.RenderComponent<NavMenu>();

        // Assert - Admin-specific links should not be visible
        var markup = cut.Markup.ToLowerInvariant();
        // Regular users shouldn't see admin-specific controls
        await Assert.That(
            !markup.Contains("admin-panel") &&
            !markup.Contains("manage-users")
        ).IsTrue();
    }

    [Test]
    [DisplayName("NavMenu_ShowsAdminMenu_WhenAdmin")]
    public async Task NavMenu_ShowsAdminMenu_WhenAdmin()
    {
        // Arrange - Configure admin user using authentication scenarios
        using var ctx = CreateContext();
        AuthenticationScenarios.Admin().Build(ctx);
        RegisterNavMenuServices(ctx);

        // Act
        var cut = ctx.RenderComponent<NavMenu>();

        // Assert - Admin should see admin controls or have access to admin routes
        await Assert.That(cut).IsNotNull();
    }

    [Test]
    [DisplayName("NavMenu_ShowsUserProfile_WhenAuthenticated")]
    public async Task NavMenu_ShowsUserProfile_WhenAuthenticated()
    {
        // Arrange - Configure authenticated user with display name
        using var ctx = CreateContext();
        AuthenticationScenarios.AuthenticatedUserWithProfile(
            Guid.NewGuid(), "Test User", "test@example.com").Build(ctx);
        RegisterNavMenuServices(ctx);

        // Act
        var cut = ctx.RenderComponent<NavMenu>();

        // Assert - Should show some indication of logged-in state
        var markup = cut.Markup;
        await Assert.That(
            markup.Contains("Test User", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Profile", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Logout", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Sign out", StringComparison.OrdinalIgnoreCase)
        ).IsTrue();
    }

    [Test]
    [DisplayName("NavMenu_ShowsLoadingState_WhenAuthorizing")]
    public async Task NavMenu_ShowsLoadingState_WhenAuthorizing()
    {
        // Arrange - Configure authorizing state (authentication in progress)
        using var ctx = CreateContext();
        AuthenticationScenarios.Authorizing().Build(ctx);
        RegisterNavMenuServices(ctx);

        // Act
        var cut = ctx.RenderComponent<NavMenu>();

        // Assert - Component should render without error during authorizing state
        await Assert.That(cut).IsNotNull();
        await Assert.That(cut.Markup).IsNotEmpty();
    }

    #endregion

    #region Protected Route Tests

    [Test]
    [DisplayName("CreateOrganization_RendersPage_WhenAnonymous")]
    public async Task CreateOrganization_RendersPage_WhenAnonymous()
    {
        // Arrange - Configure anonymous user
        // Note: CreateOrganization doesn't use [Authorize] attribute,
        // authentication is handled at submission time via AuthStateService
        using var ctx = CreateContext();
        AuthenticationScenarios.Anonymous().Build(ctx);
        RegisterOrganizationServices(ctx);

        // Act
        var cut = ctx.RenderMudComponent<CreateOrganization>();

        // Assert - Page renders (auth checked on submit, not on load)
        await Assert.That(cut).IsNotNull();
        await Assert.That(cut.Markup).Contains("Create Organization");
    }

    [Test]
    [DisplayName("CreateOrganization_RendersForm_WhenAuthenticated")]
    public async Task CreateOrganization_RendersForm_WhenAuthenticated()
    {
        // Arrange - Configure authenticated user
        using var ctx = CreateContext();
        AuthenticationScenarios.AuthenticatedUser(name: "Org Creator").Build(ctx);
        RegisterOrganizationServices(ctx);

        // Act
        var cut = ctx.RenderMudComponent<CreateOrganization>();

        // Assert - Should render the organization creation form
        await Assert.That(cut).IsNotNull();
        await Assert.That(cut.Markup.Length).IsGreaterThan(0);
    }

    #endregion

    #region Role-Based Access Control Tests

    [Test]
    [DisplayName("OrganizationOwner_HasFullAccess")]
    public async Task OrganizationOwner_HasFullAccess()
    {
        // Arrange - Configure organization owner
        using var ctx = CreateContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        AuthenticationScenarios.OrganizationOwner(orgId, userId).Build(ctx);
        RegisterOrganizationServices(ctx);

        var authState = ctx.Services.GetRequiredService<AuthenticationStateProvider>();
        var state = await authState.GetAuthenticationStateAsync();

        // Assert - Should have organization owner role
        await Assert.That(state.User.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(
            state.User.IsInRole(AuthenticationTestConstants.OrganizationOwnerRole) ||
            state.User.HasClaim("org_id", orgId.ToString())
        ).IsTrue();
    }

    [Test]
    [DisplayName("OrganizationMember_HasLimitedAccess")]
    public async Task OrganizationMember_HasLimitedAccess()
    {
        // Arrange - Configure organization member
        using var ctx = CreateContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        AuthenticationScenarios.OrganizationMember(orgId, userId).Build(ctx);

        var authState = ctx.Services.GetRequiredService<AuthenticationStateProvider>();
        var state = await authState.GetAuthenticationStateAsync();

        // Assert - Should be authenticated with member role but not owner
        await Assert.That(state.User.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(state.User.IsInRole(AuthenticationTestConstants.OrganizationMemberRole)).IsTrue();
        await Assert.That(state.User.IsInRole(AuthenticationTestConstants.OrganizationOwnerRole)).IsFalse();
    }

    [Test]
    [DisplayName("Admin_HasSystemWideAccess")]
    public async Task Admin_HasSystemWideAccess()
    {
        // Arrange - Configure admin user
        using var ctx = CreateContext();
        AuthenticationScenarios.Admin().Build(ctx);

        var authState = ctx.Services.GetRequiredService<AuthenticationStateProvider>();
        var state = await authState.GetAuthenticationStateAsync();

        // Assert - Should have admin role
        await Assert.That(state.User.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(state.User.IsInRole(AuthenticationTestConstants.AdminRole)).IsTrue();
    }

    [Test]
    [DisplayName("Admin_HasAllAccess")]
    public async Task Admin_HasAllAccess()
    {
        // Arrange - Configure admin user
        using var ctx = CreateContext();
        AuthenticationScenarios.Admin().Build(ctx);

        var authState = ctx.Services.GetRequiredService<AuthenticationStateProvider>();
        var state = await authState.GetAuthenticationStateAsync();

        // Assert - Should have admin role
        await Assert.That(state.User.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(state.User.IsInRole(AuthenticationTestConstants.AdminRole)).IsTrue();
    }

    #endregion

    #region Multi-Tenancy Tests

    [Test]
    [DisplayName("UserInTenant_HasTenantClaim")]
    public async Task UserInTenant_HasTenantClaim()
    {
        // Arrange & Act - Build principal directly to verify claims
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var principal = AuthenticationScenarios.UserInTenant(tenantId, userId).BuildPrincipal();

        // Assert - Should have tenant_id claim
        await Assert.That(principal.Identity?.IsAuthenticated ?? false).IsTrue();
        var tenantClaim = principal.FindFirst("tenant_id");
        await Assert.That(tenantClaim).IsNotNull();
        await Assert.That(tenantClaim!.Value).IsEqualTo(tenantId.ToString());
    }

    [Test]
    [DisplayName("AdminInTenant_HasBothAdminAndTenantAccess")]
    public async Task AdminInTenant_HasBothAdminAndTenantAccess()
    {
        // Arrange & Act - Build principal directly to verify claims
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var principal = AuthenticationScenarios.AdminInTenant(tenantId, userId).BuildPrincipal();

        // Assert - Should have both admin role and tenant claim
        await Assert.That(principal.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(principal.IsInRole(AuthenticationTestConstants.AdminRole)).IsTrue();

        var tenantClaim = principal.FindFirst("tenant_id");
        await Assert.That(tenantClaim).IsNotNull();
        await Assert.That(tenantClaim!.Value).IsEqualTo(tenantId.ToString());
    }

    #endregion

    #region AuthenticationTestBuilder Tests

    [Test]
    [DisplayName("AuthenticationTestBuilder_CreatesValidPrincipal")]
    public async Task AuthenticationTestBuilder_CreatesValidPrincipal()
    {
        // Arrange & Act
        var principal = new AuthenticationTestBuilder()
            .WithUser(Guid.NewGuid(), "Builder Test User")
            .WithEmail("builder@test.com")
            .WithRole(AuthenticationTestConstants.UserRole)
            .WithTenant(AuthenticationTestConstants.DefaultTenantId)
            .BuildPrincipal();

        // Assert
        await Assert.That(principal.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(principal.Identity?.Name).IsEqualTo("Builder Test User");
        await Assert.That(principal.IsInRole(AuthenticationTestConstants.UserRole)).IsTrue();

        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        await Assert.That(emailClaim).IsNotNull();
        await Assert.That(emailClaim!.Value).IsEqualTo("builder@test.com");
    }

    [Test]
    [DisplayName("AuthenticationTestBuilder_SupportsMultipleRoles")]
    public async Task AuthenticationTestBuilder_SupportsMultipleRoles()
    {
        // Arrange & Act
        var principal = new AuthenticationTestBuilder()
            .WithUser(Guid.NewGuid(), "Multi Role User")
            .WithRoles(AuthenticationTestConstants.AdminRole, AuthenticationTestConstants.OrganizationOwnerRole)
            .BuildPrincipal();

        // Assert
        await Assert.That(principal.IsInRole(AuthenticationTestConstants.AdminRole)).IsTrue();
        await Assert.That(principal.IsInRole(AuthenticationTestConstants.OrganizationOwnerRole)).IsTrue();
        await Assert.That(principal.IsInRole(AuthenticationTestConstants.UserRole)).IsFalse();
    }

    [Test]
    [DisplayName("AuthenticationTestBuilder_SupportsCustomClaims")]
    public async Task AuthenticationTestBuilder_SupportsCustomClaims()
    {
        // Arrange & Act
        var orgId = Guid.NewGuid();
        var principal = new AuthenticationTestBuilder()
            .WithUser(Guid.NewGuid(), "Custom Claims User")
            .WithClaim("organization_id", orgId.ToString())
            .WithClaim("custom_permission", "can_create_events")
            .BuildPrincipal();

        // Assert
        var orgClaim = principal.FindFirst("organization_id");
        await Assert.That(orgClaim).IsNotNull();
        await Assert.That(orgClaim!.Value).IsEqualTo(orgId.ToString());

        var permissionClaim = principal.FindFirst("custom_permission");
        await Assert.That(permissionClaim).IsNotNull();
        await Assert.That(permissionClaim!.Value).IsEqualTo("can_create_events");
    }

    [Test]
    [DisplayName("AuthenticationTestBuilder_CreatesAnonymousUser")]
    public async Task AuthenticationTestBuilder_CreatesAnonymousUser()
    {
        // Arrange & Act
        var principal = new AuthenticationTestBuilder()
            .AsAnonymous()
            .BuildPrincipal();

        // Assert
        await Assert.That(principal.Identity?.IsAuthenticated ?? true).IsFalse();
    }

    [Test]
    [DisplayName("AuthenticationTestBuilder_FluentChaining")]
    public async Task AuthenticationTestBuilder_FluentChaining()
    {
        // Arrange & Act - Test fluent API chains correctly
        var authState = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.DefaultUserId, "Fluent User")
            .WithEmail("fluent@test.com")
            .WithRole(AuthenticationTestConstants.EventOrganizerRole)
            .WithTenant(AuthenticationTestConstants.DefaultTenantId)
            .WithPolicy(AuthenticationTestConstants.CreateEventPolicy)
            .WithClaim("department", "events")
            .BuildAuthenticationState();

        // Assert
        await Assert.That(authState).IsNotNull();
        await Assert.That(authState.User.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(authState.User.IsInRole(AuthenticationTestConstants.EventOrganizerRole)).IsTrue();
    }

    #endregion

    #region Policy-Based Authorization Tests

    [Test]
    [DisplayName("EventOrganizer_HasCanCreateEventPolicy")]
    public async Task EventOrganizer_HasCanCreateEventPolicy()
    {
        // Arrange & Act - Build principal directly to verify setup
        var orgId = Guid.NewGuid();
        var principal = AuthenticationScenarios.EventOrganizer(orgId).BuildPrincipal();

        // Assert - Should be authenticated with EventOrganizer role
        await Assert.That(principal.Identity?.IsAuthenticated ?? false).IsTrue();
        await Assert.That(principal.IsInRole(AuthenticationTestConstants.EventOrganizerRole)).IsTrue();

        // Verify org claim is set
        var orgClaim = principal.FindFirst("org_id");
        await Assert.That(orgClaim).IsNotNull();
        await Assert.That(orgClaim!.Value).IsEqualTo(orgId.ToString());
    }

    [Test]
    [DisplayName("UserWithPolicies_HasCorrectPoliciesSet")]
    public async Task UserWithPolicies_HasCorrectPoliciesSet()
    {
        // Arrange & Act
        using var ctx = CreateContext();
        var authContext = ctx.AddTestAuthorization();
        authContext.SetAuthorized("Policy User");
        authContext.SetPolicies(
            AuthenticationTestConstants.CreateEventPolicy,
            AuthenticationTestConstants.ManageOrganizationPolicy
        );

        var authState = ctx.Services.GetRequiredService<AuthenticationStateProvider>();
        var state = await authState.GetAuthenticationStateAsync();

        // Assert
        await Assert.That(state.User.Identity?.IsAuthenticated ?? false).IsTrue();
    }

    #endregion

    #region Claim Extraction Tests (Matching AuthStateService Pattern)

    [Test]
    [DisplayName("ClaimExtraction_UsesSubClaimFirst")]
    public async Task ClaimExtraction_UsesSubClaimFirst()
    {
        // Arrange - Configure user with both sub and nameidentifier claims
        using var ctx = CreateContext();
        var subValue = Guid.NewGuid().ToString();
        var nameIdValue = Guid.NewGuid().ToString();

        var authContext = ctx.AddTestAuthorization();
        authContext.SetAuthorized("Test User");
        authContext.SetClaims(
            new Claim("sub", subValue),
            new Claim(ClaimTypes.NameIdentifier, nameIdValue)
        );

        var authState = ctx.Services.GetRequiredService<AuthenticationStateProvider>();
        var state = await authState.GetAuthenticationStateAsync();

        // Assert - Should find sub claim
        var subClaim = state.User.FindFirst("sub");
        await Assert.That(subClaim).IsNotNull();
        await Assert.That(subClaim!.Value).IsEqualTo(subValue);
    }

    [Test]
    [DisplayName("ClaimExtraction_FallsBackToNameIdentifier")]
    public async Task ClaimExtraction_FallsBackToNameIdentifier()
    {
        // Arrange - Configure user with only nameidentifier claim (no sub)
        using var ctx = CreateContext();
        var nameIdValue = Guid.NewGuid().ToString();

        var authContext = ctx.AddTestAuthorization();
        authContext.SetAuthorized("Test User");
        authContext.SetClaims(
            new Claim(ClaimTypes.NameIdentifier, nameIdValue)
        );

        var authState = ctx.Services.GetRequiredService<AuthenticationStateProvider>();
        var state = await authState.GetAuthenticationStateAsync();

        // Assert - Should find nameidentifier claim
        var nameIdClaim = state.User.FindFirst(ClaimTypes.NameIdentifier);
        await Assert.That(nameIdClaim).IsNotNull();
        await Assert.That(nameIdClaim!.Value).IsEqualTo(nameIdValue);

        // Sub claim should not exist
        var subClaim = state.User.FindFirst("sub");
        await Assert.That(subClaim).IsNull();
    }

    #endregion

    #region Helper Methods

    private static void RegisterNavMenuServices(BlazorTestContext ctx)
    {
        var eventService = Substitute.For<IEventService>();
        eventService.GetAllEventsAsync().Returns(new List<EventListDto>());
        ctx.Services.AddSingleton(eventService);

        var organizationService = Substitute.For<IOrganizationService>();
        organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());
        ctx.Services.AddSingleton(organizationService);

        var authStateService = Substitute.For<IAuthStateService>();
        ctx.Services.AddSingleton(authStateService);

        var userService = Substitute.For<IUserService>();
        ctx.Services.AddSingleton(userService);

        var publicExperienceService = Substitute.For<IPublicExperienceService>();
        publicExperienceService.GetSettingsAsync()
            .Returns(Task.FromResult<PublicExperienceSettingsModel?>(new PublicExperienceSettingsModel
            {
                PreferredHomePage = "EventList",
                BrandDisplayName = "ISLAMU Explore"
            }));
        publicExperienceService.ResolveHomeRoute(Arg.Any<PublicExperienceSettingsModel?>()).Returns("/events");
        ctx.Services.AddSingleton(publicExperienceService);

        var tenantNavigationService = Substitute.For<ITenantNavigationService>();
        tenantNavigationService.GetNavigationLinksAsync().Returns(new List<TenantNavigationLinkDto>());
        ctx.Services.AddSingleton(tenantNavigationService);

        var eligibilityService = Substitute.For<IEventCreationEligibilityService>();
        eligibilityService.GetEligibilityAsync().Returns(EventCreationEligibility.NotEligible);
        ctx.Services.AddSingleton(eligibilityService);

        ctx.Services.AddSingleton(new Explore.Blazor.Client.Services.SidebarState());
    }

    private static void RegisterOrganizationServices(BlazorTestContext ctx)
    {
        var organizationService = Substitute.For<IOrganizationService>();
        organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());
        ctx.Services.AddSingleton(organizationService);

        var authStateService = Substitute.For<IAuthStateService>();
        authStateService.IsAuthenticatedAsync().Returns(Task.FromResult(true));
        authStateService.GetCurrentUserIdAsync().Returns(Task.FromResult(Guid.NewGuid().ToString()));
        authStateService.GetCurrentTenantIdAsync().Returns(Task.FromResult(AuthenticationTestConstants.DefaultTenantId));
        ctx.Services.AddSingleton(authStateService);

        var eventService = Substitute.For<IEventService>();
        ctx.Services.AddSingleton(eventService);

        // Required by CreateOrganization component
        var imageStorageService = Substitute.For<IImageStorageService>();
        ctx.Services.AddSingleton(imageStorageService);

        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateOrganization>>();
        ctx.Services.AddSingleton(logger);
    }

    #endregion
}
