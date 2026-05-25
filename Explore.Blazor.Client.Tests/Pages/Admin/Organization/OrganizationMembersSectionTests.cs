// ABOUTME: Component tests for organization member HAL action affordance gating.
// ABOUTME: Verifies invite/edit/delete UI follows API-provided links instead of role inference.

using System.Text.Json;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Admin.Organization.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin.Organization;

public sealed class OrganizationMembersSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IOrganizationMemberService _memberService = Substitute.For<IOrganizationMemberService>();

    public OrganizationMembersSectionTests()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Org Admin", "admin@example.com");
        _ctx.Services.AddSingleton(_memberService);
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
        var organizationId = Guid.NewGuid();
        _memberService.GetMembersWithAffordancesAsync(organizationId)
            .Returns(new OrganizationMembersResult(
                [CreateMember(RoleHelper.OrgAdmin, withEditLink: false, withDeleteLink: false)],
                CanCreate: false));

        var cut = Render(organizationId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup.Contains("Invite Member", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Actions", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Change Role", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Remove", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task WithHalActionLinks_ShowsInviteAndRowActions()
    {
        var organizationId = Guid.NewGuid();
        _memberService.GetMembersWithAffordancesAsync(organizationId)
            .Returns(new OrganizationMembersResult(
                [CreateMember(RoleHelper.OrgAdmin, withEditLink: true, withDeleteLink: true)],
                CanCreate: true));

        var cut = Render(organizationId);
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress-circular", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Invite Member");
        await Assert.That(cut.Markup).Contains("Actions");
        await Assert.That(cut.FindAll("button.mud-menu-icon-button-activator").Count).IsEqualTo(1);
    }

    private IRenderedComponent<OrganizationMembersSection> Render(Guid organizationId)
    {
        return _ctx.RenderMudComponent<OrganizationMembersSection>(parameters => parameters
            .Add(component => component.OrganizationId, organizationId));
    }

    private static OrganizationMemberDto CreateMember(int roleId, bool withEditLink, bool withDeleteLink)
    {
        var member = new OrganizationMemberDto
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            UserEmail = "member@example.com",
            UserFullName = "Member User",
            RoleId = roleId,
            RoleName = RoleHelper.GetRoleName(roleId),
            AdditionalProperties = new Dictionary<string, object>()
        };

        var links = new List<string>();
        if (withEditLink)
        {
            links.Add("\"edit\":{\"href\":\"/api/organizationmember/role\",\"method\":\"PUT\"}");
        }

        if (withDeleteLink)
        {
            links.Add("\"delete\":{\"href\":\"/api/organizationmember/1\",\"method\":\"DELETE\"}");
        }

        if (links.Count > 0)
        {
            using var doc = JsonDocument.Parse("{" + string.Join(',', links) + "}");
            member.AdditionalProperties["_links"] = doc.RootElement.Clone();
        }

        return member;
    }
}
