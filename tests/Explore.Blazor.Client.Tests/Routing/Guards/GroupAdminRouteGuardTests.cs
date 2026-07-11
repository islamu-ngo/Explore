// ABOUTME: Unit tests for GroupAdminRouteGuard that restricts group settings routes to group admins.
// ABOUTME: Verifies role-based checks against GroupMember records for the targeted group route.

using Blazouter.Models;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Routing.Guards;
using Explore.Blazor.Client.Tests.Common.Authentication;

namespace Explore.Blazor.Client.Tests.Routing.Guards;

public class GroupAdminRouteGuardTests
{
    private static readonly Guid TestGroupId = Guid.Parse("6a5922c6-a7f6-4978-a9cd-76e136e84403");

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
        var groupService = Substitute.For<IGroupService>();

        var guard = new GroupAdminRouteGuard(authStateProvider, groupService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = $"/admin/group/{TestGroupId}/settings" });

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_InvalidGroupPath_ReturnsFalse()
    {
        // Arrange
        var principal = new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.AdminUserId, "User")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));
        var groupService = Substitute.For<IGroupService>();

        var guard = new GroupAdminRouteGuard(authStateProvider, groupService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = "/admin/group/not-a-guid/settings" });

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanActivateAsync_GroupCreatorMembership_ReturnsTrue()
    {
        // Arrange
        var userId = AuthenticationTestConstants.AdminUserId;
        var principal = new AuthenticationTestBuilder()
            .WithUser(userId, "Creator")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var groupService = Substitute.For<IGroupService>();
        groupService.GetGroupMembersAsync(TestGroupId).Returns(new List<GroupMemberDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                GroupId = TestGroupId,
                UserId = userId,
                RoleId = RoleHelper.GroupCreator
            }
        });

        var guard = new GroupAdminRouteGuard(authStateProvider, groupService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = $"/admin/group/{TestGroupId}/settings" });

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanActivateAsync_GroupAdminMembership_ReturnsTrue()
    {
        // Arrange
        var userId = AuthenticationTestConstants.AdminUserId;
        var principal = new AuthenticationTestBuilder()
            .WithUser(userId, "Admin")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var groupService = Substitute.For<IGroupService>();
        groupService.GetGroupMembersAsync(TestGroupId).Returns(new List<GroupMemberDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                GroupId = TestGroupId,
                UserId = userId,
                RoleId = RoleHelper.GroupAdmin
            }
        });

        var guard = new GroupAdminRouteGuard(authStateProvider, groupService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = $"/admin/group/{TestGroupId}/settings" });

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanActivateAsync_NonAdminMembership_ReturnsFalse()
    {
        // Arrange
        var userId = AuthenticationTestConstants.DefaultUserId;
        var principal = new AuthenticationTestBuilder()
            .WithUser(userId, "Member")
            .BuildPrincipal();

        var authState = new AuthenticationState(principal);
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var groupService = Substitute.For<IGroupService>();
        groupService.GetGroupMembersAsync(TestGroupId).Returns(new List<GroupMemberDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                GroupId = TestGroupId,
                UserId = userId,
                RoleId = RoleHelper.GroupMember
            }
        });

        var guard = new GroupAdminRouteGuard(authStateProvider, groupService);

        // Act
        var result = await guard.CanActivateAsync(new RouteMatch { MatchedPath = $"/admin/group/{TestGroupId}/settings" });

        // Assert
        await Assert.That(result).IsFalse();
    }
}
