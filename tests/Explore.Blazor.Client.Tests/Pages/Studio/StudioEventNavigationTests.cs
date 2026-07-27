// ABOUTME: bUnit coverage for HAL-driven event-level Studio navigation.
// ABOUTME: Verifies relation mapping, actor-navigation replacement, and the actor-level back link.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Shell.Workspaces;
using Explore.Blazor.Client.Pages.Studio;
using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Tests.Pages.Studio;

public sealed class StudioEventNavigationTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IEventService _eventService;

    public StudioEventNavigationTests()
    {
        _eventService = _ctx.AddMockService<IEventService>();
        _ctx.Services.AddScoped<StudioEventContextState>();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    [Arguments("edit", "Details")]
    [Arguments("publish-readiness", "Publication")]
    [Arguments("sessions", "Schedule")]
    [Arguments("configure-participation", "Registration")]
    [Arguments("team", "Team")]
    [Arguments("delete", "Danger zone")]
    public async Task Render_ShowsSectionOnlyWhenMappedHalRelationExists(string relation, string expectedLabel)
    {
        var resource = CreateEvent(relation);
        _eventService.GetEventByIdAsync(resource.Id!.Value).Returns(resource);

        var cut = _ctx.RenderMudComponent<StudioEventNavigation>(parameters => parameters
            .Add(component => component.EventId, resource.Id.Value));

        cut.WaitForAssertion(() => cut.FindAll("a").Any(link => link.TextContent.Contains(expectedLabel, StringComparison.Ordinal)));
        await Assert.That(cut.FindAll("[data-event-section]").Count).IsEqualTo(1);
        await Assert.That(cut.Markup).Contains("All events");
        await Assert.That(cut.Markup).DoesNotContain("studio-actor-switcher");
    }

    [Test]
    public async Task Render_WithLegacyRegistrationRelation_OmitsRegistrationSection()
    {
        var resource = CreateEvent("registration");
        _eventService.GetEventByIdAsync(resource.Id!.Value).Returns(resource);

        var cut = _ctx.RenderMudComponent<StudioEventNavigation>(parameters => parameters
            .Add(component => component.EventId, resource.Id.Value));

        cut.WaitForAssertion(() => cut.Find("[data-testid='studio-event-navigation']"));
        await Assert.That(cut.FindAll("[data-event-section='registration']")).IsEmpty();
        await Assert.That(cut.Markup).DoesNotContain("Registration");
    }

    [Test]
    public async Task Render_TeamSection_UsesCanonicalPublicEventRouteInsteadOfRawGuidRoute()
    {
        var resource = CreateEvent("team");
        _eventService.GetEventByIdAsync(resource.Id!.Value).Returns(resource);

        var cut = _ctx.RenderMudComponent<StudioEventNavigation>(parameters => parameters
            .Add(component => component.EventId, resource.Id.Value));

        cut.WaitForAssertion(() => cut.Find("[data-event-section='team']"));
        await Assert.That(cut.FindAll("a[href='/events/community-gathering-EVT123']").Count).IsEqualTo(1);
    }

    private static EventDto CreateEvent(string relation)
    {
        var eventId = Guid.CreateVersion7();
        var resource = new EventDto
        {
            Id = eventId,
            Title = "Community gathering",
            Slug = "community-gathering",
            PublicCode = "EVT123",
            EventStatusFullName = "Draft"
        };
        resource.AdditionalProperties["_links"] = JsonSerializer.SerializeToElement(
            new Dictionary<string, object>
            {
                [relation] = new { href = $"/api/event/{eventId}", method = "GET" }
            });
        return resource;
    }
}
