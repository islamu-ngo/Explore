// ABOUTME: Component tests for shared event-sidebar affordances and its event-image lightbox integration.
// ABOUTME: Verifies HAL-gated registration, accessible image triggers, and outside-image dismissal behavior.

using System.Text.Json;
using Explore.Blazor.Client.Components.Events;
using Explore.Blazor.Client.Services;
using MudBlazor;

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

    [Test]
    public async Task Render_WhenActualEventImageIsReady_ShowsAccessibleLightboxTrigger()
    {
        var eventId = Guid.NewGuid();
        var eventItem = CreateEventListItem(eventId);
        eventItem.FeaturedImageUri = "https://example.test/event-image.webp";

        var cut = _ctx.RenderMudComponent<EventDetailsSidebar>(parameters => parameters
            .Add(component => component.SelectedEvent, eventItem)
            .Add(component => component.EventDetail, CreateEventDetail(eventId, includeRegisterLink: false))
            .Add(component => component.IsDetailImageLoading, false));

        await Assert.That(cut.Markup).Contains("aria-label=\"View full-size image for Registration Affordance Event\"");
        await Assert.That(cut.Markup).Contains("aria-haspopup=\"dialog\"");
    }

    [Test]
    public async Task Render_WhenEventUsesFallbackImage_ShowsAccessibleLightboxTrigger()
    {
        var eventId = Guid.NewGuid();

        var cut = _ctx.RenderMudComponent<EventDetailsSidebar>(parameters => parameters
            .Add(component => component.SelectedEvent, CreateEventListItem(eventId))
            .Add(component => component.EventDetail, CreateEventDetail(eventId, includeRegisterLink: false)));

        await Assert.That(cut.Markup).Contains("aria-label=\"View full-size image for Registration Affordance Event\"");
        await Assert.That(cut.Markup).Contains("aria-haspopup=\"dialog\"");
    }

    [Test]
    public async Task LightboxDialog_ClickOutsideImage_ClosesDialog()
    {
        _ctx.Render<MudPopoverProvider>();
        var provider = _ctx.Render<MudDialogProvider>();
        var dialogService = _ctx.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<EventImageLightboxDialog>
        {
            { component => component.ImageSource, "https://example.test/event-image.webp" },
            { component => component.Alt, "Registration Affordance Event" }
        };

        var dialog = await dialogService.ShowAsync<EventImageLightboxDialog>(
            string.Empty,
            parameters,
            DialogOptionsFactory.ImageLightbox());
        var surface = provider.WaitForElement(".event-image-lightbox-dialog__surface");

        await Assert.That(provider.Markup).Contains("alt=\"Registration Affordance Event\"");
        await Assert.That(provider.Markup).DoesNotContain("Close full-size image");
        surface.Click();

        await Assert.That((await dialog.Result).Canceled).IsTrue();
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
