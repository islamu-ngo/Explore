// ABOUTME: Component tests for MyEvents auth-sensitive loading/error/empty/success states.
// ABOUTME: Verifies resilient rendering for parallel data load and event list presentation.

using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Helpers;
using MudBlazor;
using System.Reflection;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public class MyEventsTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;
    private readonly IOrganizationService _organizationService;
    private readonly IDialogService _dialogService;
    private readonly ISnackbar _snackbar;

    public MyEventsTests()
    {
        _ctx = new BlazorTestContext();
        _eventService = Substitute.For<IEventService>();
        _organizationService = Substitute.For<IOrganizationService>();
        _dialogService = Substitute.For<IDialogService>();
        _snackbar = Substitute.For<ISnackbar>();

        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_organizationService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(_dialogService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<MyEvents>>());
        _ctx.Services.AddSingleton(Substitute.For<IAccessibilityFocusService>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User", "test@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task MyEvents_ShowsLoadingState_WhileDataIsPending()
    {
        // Arrange
        var pendingEvents = new TaskCompletionSource<ICollection<EventListDto>>();
        _eventService.GetMyEventsAsync().Returns(pendingEvents.Task);
        _organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());

        // Act
        var cut = _ctx.RenderMudComponent<MyEvents>();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading your events...");

        // Cleanup
        pendingEvents.TrySetResult(new List<EventListDto>());
    }

    [Test]
    public async Task MyEvents_ShowsEmptyState_WhenNoEventsReturned()
    {
        // Arrange
        _eventService.GetMyEventsAsync().Returns(new List<EventListDto>());
        _organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());

        // Act
        var cut = _ctx.RenderMudComponent<MyEvents>();
        cut.WaitForState(() => cut.Markup.Contains("No events found", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("No events found");
        await Assert.That(cut.Markup).Contains("Create an event to get started");
    }

    [Test]
    public async Task MyEvents_ShowsErrorState_WhenLoadFails()
    {
        // Arrange
        _eventService.GetMyEventsAsync().ThrowsAsync(new InvalidOperationException("boom"));
        _organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());

        // Act
        var cut = _ctx.RenderMudComponent<MyEvents>();
        cut.WaitForState(() => cut.Markup.Contains("Unable to load your events", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Unable to load your events. Please try again.");
        await Assert.That(cut.Markup).Contains("Retry");
        _snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Unable to load your events", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }

    [Test]
    public async Task MyEvents_ShowsEventCards_WhenEventsExist()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var events = new List<EventListDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Community Iftar",
                Description = "Bring your family",
                EventTypeFullName = "Social",
                ActorId = actorId,
                ActorDisplayName = "Community Org",
                EventFormatFullName = "In-Person"
            }
        };

        var organizations = new List<OrganizationListDto>
        {
            new()
            {
                Id = actorId,
                FullName = "Community Org",
                CurrentUserRole = RoleEnum.OrgAdmin
            }
        };

        _eventService.GetMyEventsAsync().Returns(events);
        _organizationService.GetMyOrganizationsAsync().Returns(organizations);

        // Act
        var cut = _ctx.RenderMudComponent<MyEvents>();
        cut.WaitForState(() => cut.Markup.Contains("Community Iftar", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Community Iftar");
        await Assert.That(cut.Markup).Contains("1 event(s)");
    }

    [Test]
    public async Task PublishEvent_LoadsDetailConcurrencyStampBeforePublishing()
    {
        var eventId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var concurrencyStamp = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var draft = new EventListDto
        {
            Id = eventId,
            Title = "Community Iftar",
            ActorId = actorId,
            EventStatusId = 1
        };

        _eventService.GetMyEventsAsync().Returns(new List<EventListDto> { draft });
        _organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>
        {
            new()
            {
                Id = actorId,
                FullName = "Community Org",
                CurrentUserRole = RoleEnum.OrgAdmin
            }
        });
        _dialogService.ShowMessageBoxAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DialogOptions>())
            .Returns(true);
        _eventService.GetEventByIdAsync(eventId).Returns(new EventDto
        {
            Id = eventId,
            Title = draft.Title,
            ConcurrencyStamp = concurrencyStamp
        });
        _eventService.PublishEventAsync(eventId, concurrencyStamp, Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = eventId });

        var cut = _ctx.RenderMudComponent<MyEvents>();
        cut.WaitForState(() => cut.Markup.Contains("Community Iftar", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "PublishEvent", draft));

        await _eventService.Received(1).GetEventByIdAsync(eventId);
        await _eventService.Received(1).PublishEventAsync(eventId, concurrencyStamp, Arg.Any<CancellationToken>());
        await Assert.That(draft.EventStatusId).IsEqualTo(2);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");
        if (method.Invoke(instance, args.Length == 0 ? null : args) is Task task)
        {
            await task;
        }
    }
}
