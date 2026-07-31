// ABOUTME: bUnit coverage for actor and event Studio registration-order surfaces.
// ABOUTME: Verifies event-level HAL gating and distinguishes unavailable order reads from a true empty collection.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Shell.Workspaces;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Contracts.Services.Shell;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Pages.Studio;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Shell;

namespace Explore.Blazor.Client.Tests.Pages.Studio;

public sealed class StudioOrdersTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IEventService _eventService;
    private readonly IStudioContextService _studioContextService;
    private readonly UiShellState _shellState;

    public StudioOrdersTests()
    {
        _eventService = _ctx.AddMockService<IEventService>();
        _studioContextService = _ctx.AddMockService<IStudioContextService>();
        _ctx.Services.AddScoped<IWorkspaceRegistry, WorkspaceRegistry>();
        _ctx.Services.AddScoped<WorkspaceRouteClassifier>();
        _ctx.Services.AddScoped<UiShellState>();
        _shellState = _ctx.Services.GetRequiredService<UiShellState>();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task ActorOrders_OmitsEventsWithoutOrderHalRelationWithoutRequestingTheirOrders()
    {
        var actorId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        _shellState.ReconcileActiveActors([new ManagedActorDto { ActorId = actorId, DisplayName = "Community" }], actorId);
        _studioContextService.GetContextAsync(actorId, Arg.Any<CancellationToken>()).Returns(new HalResourceOfStudioContextDto
        {
            _links = new Dictionary<string, HalLink>
            {
                ["view-registration-orders"] = new() { Href = "/api/studio/context", Method = "GET" }
            }
        });
        _eventService.GetManagedEventsByActorAsync(actorId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResult([CreateEvent(eventId)])));
        _studioContextService.GetEventOrdersAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HalResourceOfRegistrationOrderDto>>([]));

        var cut = _ctx.RenderMudComponent<StudioOrders>();

        cut.WaitForElement("[data-testid='studio-orders-no-events']");
        await Assert.That(cut.FindAll("[data-testid='studio-order-list']")).IsEmpty();
        await _studioContextService.DidNotReceive().GetEventOrdersAsync(eventId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActorOrders_RendersOnlyEventsWithTheirOwnOrderHalRelation()
    {
        var actorId = Guid.CreateVersion7();
        var linkedEventId = Guid.CreateVersion7();
        var unlinkedEventId = Guid.CreateVersion7();
        _shellState.ReconcileActiveActors([new ManagedActorDto { ActorId = actorId, DisplayName = "Community" }], actorId);
        _studioContextService.GetContextAsync(actorId, Arg.Any<CancellationToken>()).Returns(new HalResourceOfStudioContextDto
        {
            _links = new Dictionary<string, HalLink>
            {
                ["view-registration-orders"] = new() { Href = "/api/studio/context", Method = "GET" }
            }
        });
        _eventService.GetManagedEventsByActorAsync(actorId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResult([CreateEvent(linkedEventId, hasOrderLink: true), CreateEvent(unlinkedEventId)])));
        _studioContextService.GetEventOrdersAsync(linkedEventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HalResourceOfRegistrationOrderDto>>([]));

        var cut = _ctx.RenderMudComponent<StudioOrders>();

        cut.WaitForElement("[data-testid='studio-orders-empty']");
        await Assert.That(cut.FindAll(".studio-orders__event")).Count().IsEqualTo(1);
        await _studioContextService.Received(1).GetEventOrdersAsync(linkedEventId, Arg.Any<CancellationToken>());
        await _studioContextService.DidNotReceive().GetEventOrdersAsync(unlinkedEventId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PersonalOrders_UsesMyEventsWhenNoActorIsActiveAndOrdersRelationIsVisible()
    {
        var eventId = Guid.CreateVersion7();
        _studioContextService.GetContextAsync(null, Arg.Any<CancellationToken>()).Returns(new HalResourceOfStudioContextDto
        {
            _links = new Dictionary<string, HalLink>
            {
                ["view-registration-orders"] = new() { Href = "/api/studio/context", Method = "GET" }
            }
        });
        _eventService.GetMyEventsPagedAsync(1, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResult([CreateEvent(eventId, hasOrderLink: true)])));
        _studioContextService.GetEventOrdersAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HalResourceOfRegistrationOrderDto>>([]));

        var cut = _ctx.RenderMudComponent<StudioOrders>();

        cut.WaitForElement("[data-testid='studio-orders-empty']");
        await Assert.That(cut.FindAll("[data-testid='studio-orders-unavailable']")).IsEmpty();
        await _eventService.Received(1).GetMyEventsPagedAsync(1, 100, Arg.Any<CancellationToken>());
        await _eventService.DidNotReceive().GetManagedEventsByActorAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _studioContextService.Received(1).GetEventOrdersAsync(eventId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActorOrders_RendersUnavailableInsteadOfEmptyWhenManagedEventReadFails()
    {
        var actorId = Guid.CreateVersion7();
        _shellState.ReconcileActiveActors([new ManagedActorDto { ActorId = actorId, DisplayName = "Community" }], actorId);
        _studioContextService.GetContextAsync(actorId, Arg.Any<CancellationToken>()).Returns(new HalResourceOfStudioContextDto
        {
            _links = new Dictionary<string, HalLink>
            {
                ["view-registration-orders"] = new() { Href = "/api/studio/context", Method = "GET" }
            }
        });
        _eventService.GetManagedEventsByActorAsync(actorId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaginatedResult<EventListDto>>(new InvalidOperationException("unavailable")));

        var cut = _ctx.RenderMudComponent<StudioOrders>();

        cut.WaitForElement("[data-testid='studio-orders-unavailable']");
        await Assert.That(cut.FindAll("[data-testid='studio-orders-no-events']")).IsEmpty();
        await Assert.That(cut.Markup).Contains("Registration orders are currently unavailable.");
    }

    [Test]
    public async Task EventOrderList_RendersUnavailableInsteadOfEmptyWhenTheOrderReadFails()
    {
        var eventId = Guid.CreateVersion7();
        _studioContextService.GetEventOrdersAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<HalResourceOfRegistrationOrderDto>>(new InvalidOperationException("unavailable")));

        var cut = _ctx.RenderMudComponent<StudioOrderList>(parameters => parameters
            .Add(component => component.EventId, eventId));

        cut.WaitForElement("[data-testid='studio-orders-unavailable']");
        await Assert.That(cut.FindAll("[data-testid='studio-orders-empty']")).IsEmpty();
        await Assert.That(cut.Markup).Contains("Registration orders are currently unavailable.");
    }

    private static EventListDto CreateEvent(Guid eventId, bool hasOrderLink = false)
    {
        var item = new EventListDto { Id = eventId, Title = "Community event" };
        item.AdditionalProperties["_links"] = JsonSerializer.SerializeToElement(
            hasOrderLink
                ? new Dictionary<string, object>
                {
                    ["view-registration-orders"] = new { href = $"/api/events/{eventId}/registration-orders" }
                }
                : new Dictionary<string, object>());
        return item;
    }

    private static PaginatedResult<EventListDto> CreateResult(IReadOnlyList<EventListDto> events) => new()
    {
        Items = events.ToList(),
        PageNumber = 1,
        PageSize = 100,
        TotalCount = events.Count
    };
}
