// ABOUTME: Component tests for EventDetail display helper behavior.
// ABOUTME: Verifies storage-backed event images render when API responses include an image id without a resolved URI.

using System.Reflection;
using System.Text.Json;
using Blazouter.Services;
using Explore.Blazor.Client.Pages.Events;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class EventDetailTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    [Test]
    public async Task GetImageUrl_WhenFeaturedImageUriMissing_UsesPublicStorageObjectUrl()
    {
        var imageId = Guid.NewGuid();
        var component = new EventDetail();
        SetProperty(component, "Navigation", _ctx.Services.GetRequiredService<NavigationManager>());
        SetField(component, "_eventDetails", new EventDto
        {
            Id = Guid.NewGuid(),
            FeaturedImageId = imageId,
            FeaturedImageUri = null
        });

        var imageUrl = InvokePrivate<string?>(component, "GetImageUrl");

        await Assert.That(imageUrl).IsNotNull();
        await Assert.That(imageUrl!).EndsWith($"/api/storageobject/{imageId}/content");
    }

    [Test]
    public async Task Render_WhenBackgroundColorMissing_KeepsThemeBackgroundAndPublishesFullBleedLayoutStyle()
    {
        var eventDto = CreateEventDto("PUBLISHED", "Published");
        eventDto.BackgroundColor = null;
        eventDto.BackgroundImageUri = "https://example.test/background.webp";
        eventDto.BackgroundEffect = "SoftOverlay";
        var appearanceState = new MainContentAppearanceState();
        RegisterEventDetailServices(eventDto);
        _ctx.Services.AddSingleton(appearanceState);

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(appearanceState.HasAppearance).IsTrue();
        await Assert.That(appearanceState.Style).Contains("--layout-padding-inline: 0px;");
        await Assert.That(appearanceState.Style.Contains("background:", StringComparison.Ordinal)).IsFalse();
        await Assert.That(appearanceState.Style.Contains("url(", StringComparison.Ordinal)).IsFalse();
        await Assert.That(appearanceState.Style.Contains("linear-gradient", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenBackgroundColorPresent_PublishesBackgroundColorOnly()
    {
        var eventDto = CreateEventDto("PUBLISHED", "Published");
        eventDto.BackgroundColor = "#123456";
        eventDto.BackgroundImageUri = "https://example.test/background.webp";
        eventDto.BackgroundEffect = "StrongOverlay";
        var appearanceState = new MainContentAppearanceState();
        RegisterEventDetailServices(eventDto);
        _ctx.Services.AddSingleton(appearanceState);

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(appearanceState.HasAppearance).IsTrue();
        await Assert.That(appearanceState.Style).Contains("--layout-padding-inline: 0px;");
        await Assert.That(appearanceState.Style).Contains("background: #123456;");
        await Assert.That(appearanceState.Style.Contains("url(", StringComparison.Ordinal)).IsFalse();
        await Assert.That(appearanceState.Style.Contains("linear-gradient", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenDraftLifecycleLinksReturned_ShowsManagementTopBarActions()
    {
        RegisterEventDetailServices(CreateEventDto("DRAFT", "Draft", "edit", "publish", "cancel", "archive"));

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => cut.Markup.Contains("event-detail-action-bar", StringComparison.Ordinal), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("event-detail-wrapper--with-action-bar");
        await Assert.That(cut.Markup).Contains("Return to Edit");
        await Assert.That(cut.Markup).Contains("Publish");
        await Assert.That(cut.Markup).Contains("Cancel");
        await Assert.That(cut.Markup).Contains("Archive");
    }

    [Test]
    public async Task Render_WhenLifecycleLinksMissing_HidesManagementTopBarActions()
    {
        RegisterEventDetailServices(CreateEventDto("DRAFT", "Draft"));

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup.Contains("event-detail-action-bar", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("event-detail-wrapper--with-action-bar", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Return to Edit", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Publish", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Archive", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenOnlyModerateLinkReturned_ShowsModerateTopBarWithoutEdit()
    {
        RegisterEventDetailServices(CreateEventDto("PUBLISHED", "Published", "moderate-light"));

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => cut.Markup.Contains("event-detail-action-bar", StringComparison.Ordinal), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Moderate");
        await Assert.That(cut.Markup.Contains("Return to Edit", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains(">Edit<", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains("Cancel", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenOnlyHeavyModerateLinkReturned_ShowsHeavyRedactTopBarWithoutEdit()
    {
        RegisterEventDetailServices(CreateEventDto("DRAFT", "Draft", "moderate-heavy"));

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => cut.Markup.Contains("event-detail-action-bar", StringComparison.Ordinal), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Heavy Redact");
        await Assert.That(cut.Markup.Contains("Return to Edit", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains(">Edit<", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains(">Moderate<", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenLightAndHeavyModerationLinksReturned_ShowsBothModerationActions()
    {
        RegisterEventDetailServices(CreateEventDto("PUBLISHED", "Published", "moderate-light", "moderate-heavy"));

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => cut.Markup.Contains("event-detail-action-bar", StringComparison.Ordinal), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Moderate");
        await Assert.That(cut.Markup).Contains("Heavy Redact");
        await Assert.That(cut.Markup.Contains("Return to Edit", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenOnlyUnmoderateLinkReturned_ShowsRestoreTopBarWithoutEdit()
    {
        RegisterEventDetailServices(CreateEventDto("MODERATED", "Moderated", "unmoderate"));

        var cut = _ctx.RenderMudComponent<EventDetail>();
        cut.WaitForState(() => cut.Markup.Contains("event-detail-action-bar", StringComparison.Ordinal), TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains("Restore");
        await Assert.That(cut.Markup.Contains("Return to Edit", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains(">Edit<", StringComparison.Ordinal)).IsFalse();
        await Assert.That(cut.Markup.Contains(">Moderate<", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Render_WhenEventAgendaHasMultipleSessions_ShowsSessionDetailLinks()
    {
        var eventId = Guid.NewGuid();
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        var eventDto = CreateEventDto("PUBLISHED", "Published");
        eventDto.Id = eventId;
        eventDto.SessionCount = 2;

        var sessions = new List<EventSessionListDto>
        {
            new()
            {
                Id = firstSessionId,
                EventId = eventId,
                Title = "Opening class",
                EventSessionStatusFullName = "Published",
                EventSessionStatusMasterCode = "PUBLISHED",
                StartTime = new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero)
            },
            new()
            {
                Id = secondSessionId,
                EventId = eventId,
                Title = "Workshop",
                EventSessionStatusFullName = "Draft",
                EventSessionStatusMasterCode = "DRAFT",
                StartTime = new DateTimeOffset(2026, 6, 25, 10, 0, 0, TimeSpan.Zero)
            }
        };

        RegisterEventDetailServices(
            eventDto,
            sessions,
            [
                new EventDayListDto
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    LocalDate = new DateOnly(2026, 6, 25),
                    Label = "Day 1"
                }
            ],
            [
                new EventAgendaItemListDto
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Title = "Doors open",
                    LocalStartDate = new DateOnly(2026, 6, 25),
                    LocalStartTime = new TimeOnly(8, 30),
                    LocalEndTime = new TimeOnly(9, 0),
                    StartTime = new DateTimeOffset(2026, 6, 25, 8, 30, 0, TimeSpan.Zero),
                    EndTime = new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero)
                }
            ]);

        var cut = _ctx.RenderMudComponent<EventDetail>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains($"/events/{eventId}/sessions/{firstSessionId}", StringComparison.Ordinal))
                throw new InvalidOperationException("First session detail link was not rendered in the agenda section.");
        }, TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup).Contains($"/events/{eventId}/sessions/{secondSessionId}");
        await Assert.That(cut.Markup).Contains("Doors open");
    }


    [Test]
    public async Task RefreshRestoredEventDetailsAsync_WhenFreshHalLinksArrive_EnablesManagementTopBar()
    {
        var eventId = Guid.NewGuid();
        var restoredEvent = CreateEventDto("DRAFT", "Draft");
        restoredEvent.Id = eventId;

        var refreshedEvent = CreateEventDto("DRAFT", "Draft", "edit", "publish", "cancel", "archive");
        refreshedEvent.Id = eventId;

        var eventService = Substitute.For<IEventService>();
        eventService.GetEventByIdAsync(eventId).Returns(refreshedEvent);

        var component = new EventDetail();
        SetProperty(component, "EventId", eventId);
        SetProperty(component, "EventService", eventService);
        SetProperty(component, "MainContentAppearanceState", new MainContentAppearanceState());
        SetProperty(component, "Logger", Substitute.For<ILogger<EventDetail>>());
        SetField(component, "_eventDetails", restoredEvent);
        SetField(component, "_isCheckingAuth", false);

        await InvokePrivateTaskAsync(component, "RefreshRestoredEventDetailsAsync");

        await Assert.That(GetProperty<bool>(component, "HasManagementTopBar")).IsTrue();
    }

    [Test]
    public async Task CheckRegistrationStatusAsync_WhenRegistrationCarriesEventId_MarksUserRegistered()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        _ctx.SetAuthenticatedUser(userId, "Registered User", "registered@example.test");

        var eventService = Substitute.For<IEventService>();
        eventService.GetRegistrationsByUserAsync(userId).Returns(
        [
            new EventRegistrationListDto
            {
                Id = registrationId,
                EventId = eventId,
                UserId = userId
            }
        ]);

        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns(new UserDto { Id = userId });

        var component = new EventDetail();
        SetProperty(component, "EventId", eventId);
        SetProperty(component, "EventService", eventService);
        SetProperty(component, "UserService", userService);
        SetProperty(component, "AuthStateProvider", _ctx.Services.GetRequiredService<AuthenticationStateProvider>());
        SetProperty(component, "Logger", Substitute.For<ILogger<EventDetail>>());

        await InvokePrivateTaskAsync(component, "CheckRegistrationStatusAsync");

        await Assert.That(GetField<bool>(component, "_isUserRegistered")).IsTrue();
        await Assert.That(GetField<List<Guid>>(component, "_userRegistrationIds")).Contains(registrationId);
    }

    public void Dispose() => _ctx.Dispose();

    private void RegisterEventDetailServices(
        EventDto eventDto,
        ICollection<EventSessionListDto>? sessions = null,
        ICollection<EventDayListDto>? days = null,
        ICollection<EventAgendaItemListDto>? eventAgendaItems = null,
        ICollection<EventSessionAgendaItemListDto>? sessionAgendaItems = null)
    {
        _ctx.SetAnonymousUser();
        _ctx.JSInterop.SetupVoid("window.scrollTo", _ => true).SetVoidResult();

        var eventService = Substitute.For<IEventService>();
        eventService.GetEventByIdAsync(Arg.Any<Guid>()).Returns(eventDto);
        eventService.GetSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<bool>())
            .Returns(sessions ?? new List<EventSessionListDto>());

        var eventDayService = Substitute.For<IEventDayService>();
        eventDayService.GetDaysByEventAsync(Arg.Any<Guid>())
            .Returns(days ?? new List<EventDayListDto>());

        var eventAgendaItemService = Substitute.For<IEventAgendaItemService>();
        eventAgendaItemService.GetAgendaItemsByEventAsync(Arg.Any<Guid>())
            .Returns(eventAgendaItems ?? new List<EventAgendaItemListDto>());

        var sessionAgendaItemService = Substitute.For<IEventSessionAgendaItemService>();
        sessionAgendaItemService.GetAgendaItemsBySessionAsync(Arg.Any<Guid>())
            .Returns(sessionAgendaItems ?? new List<EventSessionAgendaItemListDto>());

        _ctx.Services.AddSingleton(eventService);
        _ctx.Services.AddSingleton(Substitute.For<IMapsService>());
        _ctx.Services.AddScoped<RouterStateService>();
        _ctx.Services.AddSingleton(Substitute.For<IUserService>());
        _ctx.Services.AddSingleton(Substitute.For<IEventAspectService>());
        _ctx.Services.AddSingleton(sessionAgendaItemService);
        _ctx.Services.AddSingleton(eventAgendaItemService);
        _ctx.Services.AddSingleton(eventDayService);
        _ctx.Services.AddSingleton(Substitute.For<IActorSubscriptionService>());
        _ctx.Services.AddSingleton(Substitute.For<ITagService>());
        _ctx.Services.AddSingleton(Substitute.For<ICategoryService>());
        _ctx.Services.AddScoped<MainContentAppearanceState>();
        _ctx.Services.AddSingleton(Substitute.For<ILogger<EventDetail>>());
    }

    private static EventDto CreateEventDto(string statusCode, string statusName, params string[] linkRels)
    {
        return new EventDto
        {
            Id = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            Title = "Community Program",
            Content = "A community event.",
            ActorId = Guid.NewGuid(),
            ActorDisplayName = "ISLAMU",
            ActorTypeId = 2,
            ActorTypeFullName = "Organization",
            EventTypeFullName = "Program",
            EventStatusId = statusCode switch
            {
                "PUBLISHED" => 2,
                "MODERATED" => 6,
                _ => 1
            },
            EventStatusFullName = statusName,
            EventStatusMasterCode = statusCode,
            EventFormatId = 1,
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON",
            VisibilityTypeId = 1,
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            FirstSessionDate = DateTimeOffset.UtcNow.Date,
            LastSessionDate = DateTimeOffset.UtcNow.Date,
            AdditionalProperties = CreateHalLinks(linkRels)
        };
    }

    private static Dictionary<string, object> CreateHalLinks(params string[] linkRels)
    {
        var links = string.Join(
            ",",
            linkRels.Select(rel => $"\"{rel}\":{{\"href\":\"/api/event/1\",\"method\":\"GET\"}}"));
        using var doc = JsonDocument.Parse($"{{\"_links\":{{{links}}}}}");
        return new Dictionary<string, object>
        {
            ["_links"] = doc.RootElement.GetProperty("_links").Clone()
        };
    }

    private static T InvokePrivate<T>(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");

        return (T?)method.Invoke(instance, null)
            ?? throw new InvalidOperationException($"Method {methodName} returned null.");
    }

    private static async Task InvokePrivateTaskAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");

        var task = method.Invoke(instance, null) as Task
            ?? throw new InvalidOperationException($"Method {methodName} did not return a task.");
        await task;
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");

        return (T?)property.GetValue(instance)
            ?? throw new InvalidOperationException($"Property {propertyName} returned null.");
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} was not found.");

        return (T?)field.GetValue(instance)
            ?? throw new InvalidOperationException($"Field {fieldName} returned null.");
    }

    private static void SetField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} was not found.");
        field.SetValue(instance, value);
    }

    private static void SetProperty<T>(object instance, string propertyName, T value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(instance, value);
    }
}
