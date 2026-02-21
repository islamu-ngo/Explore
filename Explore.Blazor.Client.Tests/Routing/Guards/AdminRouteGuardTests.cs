// ABOUTME: Unit tests for AdminRouteGuard that restricts /admin/* routes to users with DB-backed admin claims.
// Verifies claim-based and onboarding-fallback authorization for instance admin access only.

using Blazouter.Models;
using Explore.Blazor.Client.Routing.Guards;
using Explore.Blazor.Client.Tests.Common.Authentication;

namespace Explore.Blazor.Client.Tests.Routing.Guards;

public class AdminRouteGuardTests
{
    [Test]
    public async Task CanActivateAsync_UnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .AsAnonymous()
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "/admin" };

        // Act
        var result = await guard.CanActivateAsync(routeMatch);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_AuthenticatedUserWithoutAdminClaims_ReturnsFalse()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.DefaultUserId, "Regular User")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "/admin" };

        // Act
        var result = await guard.CanActivateAsync(routeMatch);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_UserWithInstanceAdminClaim_ReturnsTrue()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "Instance Admin")
            .WithClaim("explore:admin:instance", "true")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "/admin" };

        // Act
        var result = await guard.CanActivateAsync(routeMatch);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanActivateAsync_UserWithTenantAdminClaim_ReturnsFalse()
    {
        // Arrange
        var tenantId = AuthenticationTestConstants.DefaultTenantId;
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "Tenant Admin")
            .WithClaim("explore:admin:tenant", tenantId.ToString())
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "/admin/tenant/settings" };

        // Act
        var result = await guard.CanActivateAsync(routeMatch);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_UserWithOrganizationAdminClaimOnly_ReturnsFalse()
    {
        // Arrange — organization admin alone does NOT grant /admin route access
        var orgId = Guid.NewGuid();
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.DefaultUserId, "Org Admin")
            .WithClaim("explore:admin:organization", orgId.ToString())
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "/admin" };

        // Act
        var result = await guard.CanActivateAsync(routeMatch);

        // Assert — AdminRouteGuard only checks instance authority
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_UserWithBothInstanceAndTenantClaims_ReturnsTrue()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "Full Admin")
            .WithClaim("explore:admin:instance", "true")
            .WithClaim("explore:admin:tenant", AuthenticationTestConstants.DefaultTenantId.ToString())
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "/admin" };

        // Act
        var result = await guard.CanActivateAsync(routeMatch);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task GetRedirectPathAsync_WithMatchedPath_ReturnsLoginUrlWithReturnUrl()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "/admin/tenant/settings" };

        // Act
        var redirectPath = await guard.GetRedirectPathAsync(routeMatch);

        // Assert
        await Assert.That(redirectPath).IsEqualTo("/login?returnUrl=%2Fadmin%2Ftenant%2Fsettings");
    }

    [Test]
    public async Task GetRedirectPathAsync_WithEmptyMatchedPath_ReturnsLoginWithRootReturnUrl()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = "" };

        // Act
        var redirectPath = await guard.GetRedirectPathAsync(routeMatch);

        // Assert
        await Assert.That(redirectPath).IsEqualTo("/login?returnUrl=%2F");
    }

    [Test]
    public async Task GetRedirectPathAsync_WithNullMatchedPath_ReturnsLoginWithRootReturnUrl()
    {
        // Arrange
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var guard = CreateGuard(authStateProvider);
        var routeMatch = new RouteMatch { MatchedPath = null };

        // Act
        var redirectPath = await guard.GetRedirectPathAsync(routeMatch);

        // Assert
        await Assert.That(redirectPath).IsEqualTo("/login?returnUrl=%2F");
    }

    [Test]
    public async Task CanActivateAsync_AuthenticatedUserWithoutClaims_ButInstanceStatusAdmin_ReturnsTrue()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "Instance Admin")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = true
        });

        var guard = CreateGuard(authStateProvider, instanceOnboardingService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin" });

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanActivateAsync_AuthenticatedUserWithoutClaims_ButTenantStatusAdmin_ReturnsFalse()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "Tenant Admin")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        instanceOnboardingService.GetStatusAsync().Returns((InstanceOnboardingStatusModel?)null);

        var guard = CreateGuard(authStateProvider, instanceOnboardingService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin" });

        // Assert
        await Assert.That(result).IsFalse();
    }

    private static AdminRouteGuard CreateGuard(
        AuthenticationStateProvider authStateProvider,
        IInstanceOnboardingService? instanceOnboardingService = null)
    {
        if (instanceOnboardingService is null)
        {
            instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
            instanceOnboardingService.GetStatusAsync().Returns((InstanceOnboardingStatusModel?)null);
        }

        return new AdminRouteGuard(authStateProvider, instanceOnboardingService);
    }
}
