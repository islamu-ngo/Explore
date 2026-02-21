// ABOUTME: Unit tests for TenantAdminRouteGuard that restricts tenant settings routes to tenant admins.
// ABOUTME: Verifies tenant-admin claim checks and tenant onboarding status fallback behavior.

using Blazouter.Models;
using Explore.Blazor.Client.Routing.Guards;
using Explore.Blazor.Client.Tests.Common.Authentication;

namespace Explore.Blazor.Client.Tests.Routing.Guards;

public class TenantAdminRouteGuardTests
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
        var tenantOnboardingService = Substitute.For<ITenantOnboardingService>();

        var guard = new TenantAdminRouteGuard(authStateProvider, tenantOnboardingService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin/tenant/settings" });

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_UserWithTenantClaim_ReturnsTrue()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "Tenant Admin")
            .WithClaim("explore:admin:tenant", AuthenticationTestConstants.DefaultTenantId.ToString())
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));
        var tenantOnboardingService = Substitute.For<ITenantOnboardingService>();
        tenantOnboardingService.GetStatusAsync().Returns((TenantOnboardingStatusModel?)null);

        var guard = new TenantAdminRouteGuard(authStateProvider, tenantOnboardingService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin/tenant/settings" });

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanActivateAsync_UserWithOnlyInstanceClaim_ReturnsFalse()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "Instance Admin")
            .WithClaim("explore:admin:instance", "true")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));
        var tenantOnboardingService = Substitute.For<ITenantOnboardingService>();
        tenantOnboardingService.GetStatusAsync().Returns((TenantOnboardingStatusModel?)null);

        var guard = new TenantAdminRouteGuard(authStateProvider, tenantOnboardingService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin/tenant/settings" });

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_WithoutClaims_ButTenantStatusAdmin_ReturnsTrue()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.DefaultUserId, "Tenant Admin")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));
        var tenantOnboardingService = Substitute.For<ITenantOnboardingService>();
        tenantOnboardingService.GetStatusAsync().Returns(new TenantOnboardingStatusModel
        {
            IsAuthenticated = true,
            IsCurrentUserTenantAdministrator = true
        });

        var guard = new TenantAdminRouteGuard(authStateProvider, tenantOnboardingService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin/tenant/settings" });

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanActivateAsync_WithoutClaims_ButOnlyPlatformStatusAdmin_ReturnsFalse()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.DefaultUserId, "Platform Admin")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));
        var tenantOnboardingService = Substitute.For<ITenantOnboardingService>();
        tenantOnboardingService.GetStatusAsync().Returns(new TenantOnboardingStatusModel
        {
            IsAuthenticated = true,
            IsCurrentUserPlatformAdministrator = true,
            IsCurrentUserTenantAdministrator = false
        });

        var guard = new TenantAdminRouteGuard(authStateProvider, tenantOnboardingService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin/tenant/settings" });

        // Assert
        await Assert.That(result).IsFalse();
    }
}
