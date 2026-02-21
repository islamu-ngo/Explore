// ABOUTME: Route guard that restricts group admin routes to group-scoped administrators for the specific group.
// ABOUTME: Verifies membership from GroupMember records and allows only GroupCreator and GroupAdmin roles.

using System.Security.Claims;
using System.Text.RegularExpressions;
using Blazouter.Interfaces;
using Blazouter.Models;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

public sealed partial class GroupAdminRouteGuard(
    AuthenticationStateProvider authStateProvider,
    IGroupService groupService) : IRouteGuard
{
    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = authState.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var groupId = ExtractGroupIdFromPath(match.MatchedPath);
        if (groupId == null)
        {
            return false;
        }

        var userId = ExtractUserId(user);
        if (!userId.HasValue)
        {
            return false;
        }

        try
        {
            var members = await groupService.GetGroupMembersAsync(groupId.Value).ConfigureAwait(false);
            var membership = members.FirstOrDefault(m => m.UserId == userId.Value);
            return RoleHelper.CanManageGroup(membership?.RoleId);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetRedirectPathAsync(RouteMatch match)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            return "/";
        }

        var returnUrl = string.IsNullOrWhiteSpace(match.MatchedPath)
            ? "/"
            : match.MatchedPath;

        return $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    private static Guid? ExtractUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sid")?.Value;

        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static Guid? ExtractGroupIdFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var match = GroupIdPattern().Match(path);
        if (!match.Success)
            return null;

        return Guid.TryParse(match.Groups[1].Value, out var groupId) ? groupId : null;
    }

    [GeneratedRegex(@"/group/([0-9a-fA-F\-]{36})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GroupIdPattern();
}
