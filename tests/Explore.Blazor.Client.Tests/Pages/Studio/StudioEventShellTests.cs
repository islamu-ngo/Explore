// ABOUTME: bUnit coverage for the event-scoped Studio participation route boundary.
// ABOUTME: Proves direct registration navigation fails closed without its event HAL relation.

using System.Text.Json;
using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Pages.Studio;
using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Tests.Pages.Studio;

public sealed class StudioEventShellTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IEventService _eventService;

    public StudioEventShellTests()
    {
        _eventService = _ctx.AddMockService<IEventService>();
        _ctx.Services.AddScoped<RouterStateService>();
        _ctx.Services.AddScoped<StudioEventContextState>();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task RegistrationRoute_WithoutConfigureParticipationRelation_FailsClosed()
    {
        var resource = CreateEvent();
        _eventService.GetEventByIdAsync(resource.Id!.Value).Returns(resource);
        _ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/studio/events/{resource.Id}/registration");

        var cut = _ctx.RenderMudComponent<StudioEventShell>(parameters => parameters
            .Add(component => component.EventId, resource.Id.Value));

        cut.WaitForElement("[data-testid='participation-route-unavailable']");
        await Assert.That(cut.FindAll("[data-testid='participation-configuration-editor']")).IsEmpty();
        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("Community gathering");
        await Assert.That(cut.FindAll("h1").Count).IsEqualTo(1);
    }

    [Test]
    public async Task RegistrationRoute_WithConfigureParticipationRelation_RendersEditor()
    {
        var resource = CreateEvent("configure-participation");
        _eventService.GetEventByIdAsync(resource.Id!.Value).Returns(resource);
        _ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/studio/events/{resource.Id}/registration");

        var cut = _ctx.RenderMudComponent<StudioEventShell>(parameters => parameters
            .Add(component => component.EventId, resource.Id.Value));

        cut.WaitForElement("[data-testid='participation-configuration-editor']");
        await Assert.That(cut.FindAll("[data-testid='participation-route-unavailable']")).IsEmpty();
    }

    private static EventDto CreateEvent(string? relation = null)
    {
        var eventId = Guid.CreateVersion7();
        var resource = new EventDto
        {
            Id = eventId,
            Title = "Community gathering",
            EventStatusFullName = "Draft",
            ParticipationConfiguration = new ParticipationConfiguration
            {
                EventId = eventId,
                ConcurrencyStamp = Guid.CreateVersion7(),
                ParticipationHandlingModeId = 1,
                ParticipationHandlingModeCode = "INFORMATION_ONLY",
                ParticipationHandlingModeName = "Information only",
                AdvanceRegistrationObligationId = 1,
                AdvanceRegistrationObligationCode = "NOT_APPLICABLE",
                AdvanceRegistrationObligationName = "Not applicable"
            }
        };

        if (relation is not null)
        {
            resource.AdditionalProperties["_links"] = JsonSerializer.SerializeToElement(
                new Dictionary<string, object>
                {
                    [relation] = new { href = $"/api/events/{eventId}/participation", method = "PATCH" }
                });
        }

        return resource;
    }
}
