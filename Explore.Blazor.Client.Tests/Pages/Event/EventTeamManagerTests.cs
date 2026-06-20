// ABOUTME: Component tests for EventTeamManager authorization-driven affordances.
// ABOUTME: Ensures read-only team visibility does not expose assign or revoke actions.

using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class EventTeamManagerTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    [Test]
    public async Task Render_WhenTeamVisibleButNoAssignablePresets_HidesWriteActions()
    {
        var eventId = Guid.NewGuid();
        var eventTeamService = Substitute.For<IEventTeamService>();
        eventTeamService.GetTeamMembersAsync(eventId, includeInactive: false)
            .Returns(new List<EventTeamMemberDto>
            {
                new()
                {
                    AssignmentId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    UserEmail = "manager@example.test",
                    UserFullName = "Event Manager",
                    RoleId = RoleHelper.EventManager,
                    RoleName = "Manager",
                    RoleMasterCode = "event.manager",
                    Status = EventRoleAssignmentStatus.Active,
                    StartsAtUtc = DateTimeOffset.UtcNow,
                    IsEffective = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            });
        eventTeamService.GetAssignablePresetsAsync(eventId)
            .Returns(new List<EventRolePresetDto>());
        _ctx.Services.AddSingleton(eventTeamService);

        var cut = _ctx.RenderMudComponent<EventTeamManager>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.CanManageTeam, true));

        cut.WaitForState(
            () => cut.Markup.Contains("manager@example.test", StringComparison.Ordinal),
            TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Event Team");
        await Assert.That(cut.Markup.Contains("Assign Role", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Revoke", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenAssignablePresetsExist_ShowsAssignAction()
    {
        var eventId = Guid.NewGuid();
        var eventTeamService = Substitute.For<IEventTeamService>();
        eventTeamService.GetTeamMembersAsync(eventId, includeInactive: false)
            .Returns(new List<EventTeamMemberDto>());
        eventTeamService.GetAssignablePresetsAsync(eventId)
            .Returns(new List<EventRolePresetDto>
            {
                new()
                {
                    RoleId = RoleHelper.RegistrationManager,
                    MasterCode = "event.registration_manager",
                    FullName = "Registration Manager",
                    PermissionCodes = new List<string> { "event_registration:manage" }
                }
            });
        _ctx.Services.AddSingleton(eventTeamService);

        var cut = _ctx.RenderMudComponent<EventTeamManager>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.CanManageTeam, true));

        cut.WaitForState(
            () => cut.Markup.Contains("Assign Role", StringComparison.Ordinal),
            TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Assign Role");
    }

    public void Dispose() => _ctx.Dispose();
}
