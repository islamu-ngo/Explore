// ABOUTME: Component tests for group member HAL action affordance gating.
// ABOUTME: Verifies invite/edit/delete UI follows API-provided links instead of role inference.

using System.Text.Json;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Admin.Group.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin.Group;

public sealed class GroupMembersSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IGroupService _groupService = Substitute.For<IGroupService>();

    public GroupMembersSectionTests()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Group Admin", "admin@example.com");
        _ctx.Services.AddSingleton(_groupService);
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task WithoutHalActionLinks_HidesInviteAndRowActions_EvenForAdminRole()
    {
        var groupId = Guid.NewGuid();
        _groupService.GetGroupMembersWithAffordancesAsync(groupId)
            .Returns(new GroupMembersResult(
                [CreateMember(RoleHelper.GroupAdmin, withEditLink: false, withDeleteLink: false)],
                CanCreate: false));

        var cut = Render(groupId);

        await Assert.That(cut.Markup.Contains("Invite Member", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Actions", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Save role", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Remove member", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task WithHalActionLinks_ShowsInviteAndRowActions()
    {
        var groupId = Guid.NewGuid();
        _groupService.GetGroupMembersWithAffordancesAsync(groupId)
            .Returns(new GroupMembersResult(
                [CreateMember(RoleHelper.GroupAdmin, withEditLink: true, withDeleteLink: true)],
                CanCreate: true));

        var cut = Render(groupId);

        await Assert.That(cut.Markup).Contains("Invite Member");
        await Assert.That(cut.Markup).Contains("Actions");
        await Assert.That(cut.Markup).Contains("Save role");
        await Assert.That(cut.Markup).Contains("Remove member");
    }

    private IRenderedComponent<GroupMembersSection> Render(Guid groupId)
    {
        return _ctx.RenderMudComponent<GroupMembersSection>(parameters => parameters
            .Add(component => component.GroupId, groupId));
    }

    private static GroupMemberDto CreateMember(int roleId, bool withEditLink, bool withDeleteLink)
    {
        var member = new GroupMemberDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            UserEmail = "member@example.com",
            UserFullName = "Member User",
            RoleId = roleId,
            RoleName = RoleHelper.GetGroupRoleName(roleId),
            AdditionalProperties = new Dictionary<string, object>()
        };

        var links = new List<string>();
        if (withEditLink)
        {
            links.Add("\"edit\":{\"href\":\"/api/groupmember/1\",\"method\":\"PUT\"}");
        }

        if (withDeleteLink)
        {
            links.Add("\"delete\":{\"href\":\"/api/groupmember/1\",\"method\":\"DELETE\"}");
        }

        if (links.Count > 0)
        {
            using var doc = JsonDocument.Parse("{" + string.Join(',', links) + "}");
            member.AdditionalProperties["_links"] = doc.RootElement.Clone();
        }

        return member;
    }
}
