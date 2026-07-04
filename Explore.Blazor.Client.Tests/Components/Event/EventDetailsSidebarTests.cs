// ABOUTME: Component tests for the shared event details sidebar registration affordance.
// ABOUTME: Verifies registration actions are rendered from HAL links instead of local role assumptions.

using System.Text.Json;
using Explore.Blazor.Client.Components.Events;

namespace Explore.Blazor.Client.Tests.Components.Event;

public sealed class EventDetailsSidebarTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task Render_WhenRegisterHalLinkExists_ShowsRegisterAction()
    {
        var eventId = Guid.NewGuid();

        var cut = _ctx.RenderMudComponent<EventDetailsSidebar>(parameters => parameters
            .Add(component => component.SelectedEvent, CreateEventListItem(eventId))
            .Add(component => component.EventDetail, CreateEventDetail(eventId, includeRegisterLink: true)));

        await Assert.That(cut.Markup).Contains("Register for this Event");
    }

    [Test]
    public async Task Render_WhenRegisterHalLinkIsMissing_HidesRegisterAction()
    {
        var eventId = Guid.NewGuid();

        var cut = _ctx.RenderMudComponent<EventDetailsSidebar>(parameters => parameters
            .Add(component => component.SelectedEvent, CreateEventListItem(eventId))
            .Add(component => component.EventDetail, CreateEventDetail(eventId, includeRegisterLink: false)));

        await Assert.That(cut.Markup).DoesNotContain("Register for this Event");
    }

    private static EventListDto CreateEventListItem(Guid eventId) => new()
    {
        Id = eventId,
        Title = "Registration Affordance Event",
        Description = "An event used to verify HAL-gated registration affordances.",
        EventTypeFullName = "Conference",
        EventStatusFullName = "Published",
        FirstSessionDate = DateTimeOffset.UtcNow.AddDays(7)
    };

    private static EventDto CreateEventDetail(Guid eventId, bool includeRegisterLink) => new()
    {
        Id = eventId,
        Title = "Registration Affordance Event",
        Description = "An event used to verify HAL-gated registration affordances.",
        EventStatusMasterCode = "PUBLISHED",
        IsRegistrationRequired = true,
        AdditionalProperties = includeRegisterLink
            ? CreateHalLinks("register")
            : []
    };

    private static Dictionary<string, object> CreateHalLinks(params string[] linkRels)
    {
        var links = string.Join(
            ',',
            linkRels.Select(rel => $"\"{rel}\":{{\"href\":\"/api/eventregistration\",\"method\":\"POST\"}}"));
        using var doc = JsonDocument.Parse($"{{\"_links\":{{{links}}}}}");

        return new Dictionary<string, object>
        {
            ["_links"] = doc.RootElement.GetProperty("_links").Clone()
        };
    }
}
