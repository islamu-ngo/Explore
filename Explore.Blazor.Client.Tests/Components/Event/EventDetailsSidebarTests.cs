// ABOUTME: bUnit tests for the shared event details sidebar action states.
// ABOUTME: Verifies past events render passive archive affordances instead of active prompts.

using Explore.Blazor.Client.Components.Events;

namespace Explore.Blazor.Client.Tests.Components.Event;

public sealed class EventDetailsSidebarTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public EventDetailsSidebarTests()
    {
        _ctx = new BlazorTestContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task Sidebar_WhenSelectedEventIsPast_ShowsEndedAndSuppressesPublicActions()
    {
        var selectedEvent = new EventListDto
        {
            Id = Guid.NewGuid(),
            Title = "Past Community Lecture",
            EventTypeFullName = "Lecture",
            EventStatusFullName = "Published",
            IsPast = true
        };

        var cut = _ctx.RenderMudComponent<EventDetailsSidebar>(p => p
            .Add(x => x.SelectedEvent, selectedEvent)
            .Add(x => x.IsUserRegistered, false));

        await Assert.That(cut.Markup).Contains("Ended");
        await Assert.That(cut.Markup).DoesNotContain("Event Page");
        await Assert.That(cut.Markup).DoesNotContain("Copy Link");
        await Assert.That(cut.Markup).DoesNotContain("Register for this Event");
    }
}
