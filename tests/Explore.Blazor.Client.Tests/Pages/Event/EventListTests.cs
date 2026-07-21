// ABOUTME: Focused bUnit tests for EventList loading and empty-state behavior.
// ABOUTME: Verifies stable UX state transitions with Virtualize-backed API paging.

using System.Reflection;
using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Services.Docking;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public class EventListTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly IAdminService _adminService;
    private readonly ILocationService _locationService;
    private readonly IEventRegistrationService _registrationService;
    private readonly IPublicExperienceService _publicExperienceService;
    private readonly DockLayoutState _dockLayoutState;
    private readonly IDockLayoutPersistence _dockLayoutPersistence;

    public EventListTests()
    {
        _ctx = new BlazorTestContext();

        _eventService = Substitute.For<IEventService>();
        _categoryService = Substitute.For<ICategoryService>();
        _tagService = Substitute.For<ITagService>();
        _adminService = Substitute.For<IAdminService>();
        _locationService = Substitute.For<ILocationService>();
        _registrationService = Substitute.For<IEventRegistrationService>();
        _publicExperienceService = Substitute.For<IPublicExperienceService>();
        _dockLayoutState = new DockLayoutState();
        _dockLayoutPersistence = Substitute.For<IDockLayoutPersistence>();
        _dockLayoutPersistence.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DockLayoutSnapshot?>(null));
        _dockLayoutPersistence.SaveAsync(Arg.Any<DockLayoutSnapshot>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _dockLayoutPersistence.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_categoryService);
        _ctx.Services.AddSingleton(_tagService);
        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(_locationService);
        _ctx.Services.AddSingleton(_registrationService);
        _ctx.Services.AddSingleton(_publicExperienceService);
        _ctx.Services.AddSingleton(_dockLayoutState);
        _ctx.Services.AddSingleton(_dockLayoutPersistence);

        _ctx.Services.AddSingleton(Substitute.For<IUserService>());
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<ILogger<EventList>>());
        _ctx.Services.AddSingleton(Substitute.For<IAuthStateService>());
        _ctx.Services.AddSingleton(Substitute.For<IContactShareConsentService>());
        _ctx.Services.AddSingleton(Substitute.For<IUserSettingsService>());
        _ctx.Services.AddSingleton(new FeatureStateContainer());

        SetupDefaultLookupResponses();
        _publicExperienceService.GetSettingsAsync().Returns(new PublicExperienceSettingsDto
        {
            IsIslamicModuleEnabled = true,
            IsTechModuleEnabled = true
        });
        _publicExperienceService.GetCachedShellAsync().Returns(Task.FromResult<PublicExperienceShellDto?>(null));
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private void SetupDefaultLookupResponses()
    {
        _adminService.GetEventTypesAsync().Returns(new List<EventTypeListDto>());
        _adminService.GetAudienceGendersAsync().Returns(new List<AudienceGenderListDto>());
        _adminService.GetAudienceAgesAsync().Returns(new List<AudienceAgeListDto>());
        _adminService.GetEventStatusesAsync().Returns(new List<EventStatusListDto>());
        _eventService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>());
        _categoryService.GetAllCategoriesAsync().Returns(new List<CategoryListDto>());
        _tagService.GetAllTagsAsync().Returns(new List<TagListDto>());
        _adminService.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        _locationService.GetAllLocationsAsync().Returns(new List<LocationListDto>());
        _adminService.GetRegistrationModesAsync().Returns(new List<RegistrationModeListDto>());
        _adminService.GetLanguagesAsync().Returns(new List<LanguageListDto>());
        _eventService.GetRegistrationsByUserAsync(Arg.Any<Guid>()).Returns(new List<EventRegistrationListDto>());
    }

    private static PaginatedResult<EventListDto> CreateResult(int pageNumber, int pageSize, List<EventListDto> items)
    {
        return new PaginatedResult<EventListDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = items.Count
        };
    }

    private static DockLayoutSnapshot CreateWorkspaceSnapshot(bool customizeOpen, int customizeWidth)
    {
        return new DockLayoutSnapshot(
            "events",
            [
                new DockPanelState(EventDockPanels.CustomizeViewId, customizeOpen, DockMode.Docked, customizeWidth, Order: 10, IsActive: customizeOpen),
                new DockPanelState(EventDockPanels.EventPreviewId, true, DockMode.Inspector, Width: 440, Order: 20, IsActive: true)
            ],
            DateTimeOffset.UtcNow);
    }

    private static bool IsExpectedAutosaveSnapshot(DockLayoutSnapshot? snapshot)
    {
        return snapshot is not null
            && snapshot.LayoutKey == "events"
            && snapshot.Panels.Count == 1
            && snapshot.Panels.Any(panel => panel.Id == EventDockPanels.CustomizeViewId && panel.IsOpen)
            && snapshot.Panels.All(panel => panel.Id != EventDockPanels.EventPreviewId)
            && snapshot.Panels.All(panel => panel.Id != ShellDockPanels.WorkspaceNavId)
            && snapshot.Panels.All(panel => panel.Id != ShellDockPanels.AiAssistantId);
    }

    private static DockPanelDescriptor CreateShellPersistentDescriptor(DockPanelId id, DockSide side)
    {
        return new DockPanelDescriptor(
            id,
            DockScope.Shell,
            side,
            DockMode.Docked,
            Title: "Shell panel",
            AriaLabel: "Shell panel",
            DefaultWidth: 320,
            MinWidth: 280,
            MaxWidth: 520,
            Order: 10,
            IsResizable: true,
            CanClose: true,
            PersistState: true);
    }

    private void SetupPagedResult(Task<PaginatedResult<EventListDto>> resultTask)
    {
        _eventService.GetEventsPagedAsync(
            Arg.Any<int>(),                    // pageNumber
            Arg.Any<int>(),                    // pageSize
            Arg.Any<string?>(),                // searchTerm
            Arg.Any<Guid?>(),                  // categoryId
            Arg.Any<List<Guid>?>(),            // includedCategoryIds
            Arg.Any<List<Guid>?>(),            // excludedCategoryIds
            Arg.Any<string?>(),                // categoryInclusionMode
            Arg.Any<string?>(),                // categoryExclusionMode
            Arg.Any<List<Guid>?>(),            // includedTagIds
            Arg.Any<List<Guid>?>(),            // excludedTagIds
            Arg.Any<string?>(),                // inclusionMode
            Arg.Any<string?>(),                // exclusionMode
            Arg.Any<List<int>?>(),             // formatIds
            Arg.Any<List<int>?>(),             // madhabIds
            Arg.Any<List<int>?>(),             // registrationModeIds
            Arg.Any<List<int>?>(),             // languageIds
            Arg.Any<DateTimeOffset?>(),        // dateFrom
            Arg.Any<DateTimeOffset?>(),        // dateTo
            Arg.Any<string?>(),                // sortBy
            Arg.Any<bool?>(),                  // sortDescending
            Arg.Any<List<int>?>(),             // eventTypeIds
            Arg.Any<List<int>?>(),             // audienceGenderIds
            Arg.Any<List<int>?>(),             // audienceAgeIds
            Arg.Any<List<int>?>(),             // eventStatusIds
            Arg.Any<List<int>?>(),             // genderModeIds
            Arg.Any<bool?>(),                  // includesQuranRecitation
            Arg.Any<List<int>?>(),             // referencePrayerIds
            Arg.Any<List<int>?>(),             // islamicPrimaryLanguageIds
            Arg.Any<bool?>(),                  // hasIslamicAspect
            Arg.Any<int?>(),                   // skillLevelId
            Arg.Any<bool?>(),                  // isCodingCompetition
            Arg.Any<bool?>(),                  // isHackathon
            Arg.Any<bool?>(),                  // requiresLaptop
            Arg.Any<string?>(),                // techStackTag
            Arg.Any<bool?>(),                  // hasTechAspect
            Arg.Any<Guid?>(),                  // actorId
            Arg.Any<Guid?>(),                  // organizationId
            Arg.Any<Guid?>(),                  // groupId
            Arg.Any<string?>(),                // view
            Arg.Any<CancellationToken>())
            .Returns(resultTask);
    }

    /// <summary>
    /// Invokes the private LoadEventsAsync Virtualize provider callback via reflection.
    /// This is an intentional workaround because bUnit cannot directly trigger a
    /// Virtualize component's ItemsProvider delegate. The method is the sole entry
    /// point for paged event loading and must be invoked to test empty-state transitions.
    /// All assertions use rendered markup (public output), not internal state.
    /// </summary>
    private static async Task InvokeLoadEventsAsync(IRenderedComponent<EventList> cut)
    {
        var loadEventsMethod = typeof(EventList)
            .GetMethod("LoadEventsAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LoadEventsAsync not found — Virtualize provider method may have been renamed.");

        var request = new ItemsProviderRequest(startIndex: 0, count: 20, cancellationToken: CancellationToken.None);
        await cut.InvokeAsync(async () =>
        {
            var valueTask = (ValueTask<ItemsProviderResult<EventListDto>>)loadEventsMethod.Invoke(cut.Instance, [request])!;
            await valueTask;
        });
    }

    private static async Task InvokePrivateTaskAsync(IRenderedComponent<EventList> cut, string methodName, params object?[] parameters)
    {
        var method = typeof(EventList)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{methodName} not found — EventList interaction contract may have changed.");

        await cut.InvokeAsync(async () =>
        {
            var task = method.Invoke(cut.Instance, parameters) as Task
                ?? throw new InvalidOperationException($"{methodName} did not return a Task.");

            await task;
        });
    }

    private static async Task<PaginatedResult<EventListDto>> InvokeFetchEventsPagedAsync(
        IRenderedComponent<EventList> cut,
        int pageNumber,
        int pageSize)
    {
        var method = typeof(EventList)
            .GetMethod("FetchEventsPagedAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FetchEventsPagedAsync not found — EventList filter forwarding contract may have changed.");

        return await cut.InvokeAsync(async () =>
        {
            var task = (Task<PaginatedResult<EventListDto>>)method.Invoke(cut.Instance, [pageNumber, pageSize, CancellationToken.None])!;
            return await task;
        });
    }

    private static async Task InvokePrivateVoidAsync(IRenderedComponent<EventList> cut, string methodName)
    {
        var method = typeof(EventList)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{methodName} not found — EventList interaction contract may have changed.");

        await cut.InvokeAsync(() => method.Invoke(cut.Instance, []));
    }

    private static async Task WaitForAsync(Action assertion, TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                assertion();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
        }

        try
        {
            assertion();
        }
        catch (Exception ex)
        {
            throw new TimeoutException("The expected assertion did not pass before the timeout.", lastException ?? ex);
        }
    }

    private static T GetPrivateField<T>(EventList instance, string fieldName)
    {
        var field = typeof(EventList)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{fieldName} not found — EventList state contract may have changed.");

        return (T)field.GetValue(instance)!;
    }

    private static void SetPrivateField<T>(EventList instance, string fieldName, T value)
    {
        var field = typeof(EventList)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{fieldName} not found — EventList state contract may have changed.");

        field.SetValue(instance, value);
    }

    private async Task RenderInlineRegistrationStateAsync(
        IRenderedComponent<EventList> cut,
        EventListDto selectedEvent,
        string stateField,
        bool isWaitlisted = false)
    {
        SetPrivateField(cut.Instance, "_selectedEvent", selectedEvent);
        SetPrivateField(cut.Instance, "_detailDrawerOpen", true);
        SetPrivateField(cut.Instance, "_showInlineRegistration", true);
        SetPrivateField(cut.Instance, stateField, true);
        SetPrivateField(cut.Instance, "_regIsWaitlisted", isWaitlisted);

        _dockLayoutState.Open(EventDockPanels.EventPreviewId);
        await cut.InvokeAsync(() => cut.Render());
    }

    private void SetupEventDetailResponses(Guid eventId)
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        SetupEventDetailResponse(eventId, "Dock Baseline Event");
    }

    private void SetupEventDetailResponse(Guid eventId, string title)
    {
        _eventService.GetEventByIdAsync(eventId).Returns(new EventDto
        {
            Id = eventId,
            Title = title,
            Description = "Event used for sidebar baseline tests"
        });

        _eventService.GetSessionsByEventAsync(eventId, Arg.Any<bool>()).Returns(new List<EventSessionListDto>());
    }

    [Test]
    public async Task FetchEventsPagedAsync_ForwardsOwnershipQueryStateToService()
    {
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/events?actorId={actorId}&organizationId={organizationId}&groupId={groupId}");

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokeFetchEventsPagedAsync(cut, 1, 20);

        await _eventService.Received().GetEventsPagedAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<List<Guid>?>(),
            Arg.Any<List<Guid>?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<List<Guid>?>(),
            Arg.Any<List<Guid>?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<string?>(),
            Arg.Any<bool?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<bool?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<List<int>?>(),
            Arg.Any<bool?>(),
            Arg.Any<int?>(),
            Arg.Any<bool?>(),
            Arg.Any<bool?>(),
            Arg.Any<bool?>(),
            Arg.Any<string?>(),
            Arg.Any<bool?>(),
            actorId,
            organizationId,
            groupId,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Render_RegistersWorkspaceDockHostAndCustomizePanelDescriptor()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));

        var cut = _ctx.RenderMudComponent<EventList>();

        var host = cut.Find("[data-testid='event-list-workspace-dock-host'][data-dock-scope='workspace']");
        var customizePanel = _dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId);
        var previewPanel = _dockLayoutState.GetPanel(EventDockPanels.EventPreviewId);

        await Assert.That(host).IsNotNull();
        await Assert.That(customizePanel).IsNotNull();
        await Assert.That(customizePanel!.Descriptor).IsEqualTo(EventDockPanels.CustomizeView);
        await Assert.That(previewPanel).IsNotNull();
        await Assert.That(previewPanel!.Descriptor).IsEqualTo(EventDockPanels.EventPreview);
        await Assert.That(cut.FindAll("[data-testid='event-list-workspace-dock-host'][data-dock-scope='workspace']").Count).IsEqualTo(1);
    }

    [Test]
    public async Task FirstRender_HydratesWorkspaceDockLayoutAfterDescriptorsRegister()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        _dockLayoutPersistence.LoadAsync("events", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DockLayoutSnapshot?>(CreateWorkspaceSnapshot(customizeOpen: true, customizeWidth: 360)));

        var cut = _ctx.RenderMudComponent<EventList>();

        cut.WaitForAssertion(() =>
        {
            var customizePanel = _dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId);
            if (customizePanel?.State is not { IsOpen: true, Width: 360 })
                throw new InvalidOperationException("Expected workspace snapshot to restore Customize View state.");

            var previewPanel = _dockLayoutState.GetPanel(EventDockPanels.EventPreviewId);
            if (previewPanel?.State.IsOpen == true)
                throw new InvalidOperationException("Expected non-persistent Event Preview state to be ignored during restore.");
        });

        await _dockLayoutPersistence.Received(1).LoadAsync("events", Arg.Any<CancellationToken>());
        await _dockLayoutPersistence.DidNotReceive().SaveAsync(Arg.Any<DockLayoutSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WorkspaceDockChange_AfterHydration_DebouncesAutosaveWithEventsKey()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));

        var cut = _ctx.RenderMudComponent<EventList>();

        cut.WaitForAssertion(() =>
            _dockLayoutPersistence.Received(1).LoadAsync("events", Arg.Any<CancellationToken>()).GetAwaiter().GetResult());

        await InvokePrivateVoidAsync(cut, "OpenCustomizationDrawer");

        await WaitForAsync(() =>
            _dockLayoutPersistence.Received(1).SaveAsync(
                Arg.Is<DockLayoutSnapshot>(snapshot => IsExpectedAutosaveSnapshot(snapshot)),
                Arg.Any<CancellationToken>()).GetAwaiter().GetResult());
    }

    [Test]
    public async Task ShellDockChange_AfterWorkspaceHydration_DoesNotAutosaveEventsLayout()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));

        var cut = _ctx.RenderMudComponent<EventList>();

        cut.WaitForAssertion(() =>
            _dockLayoutPersistence.Received(1).LoadAsync("events", Arg.Any<CancellationToken>()).GetAwaiter().GetResult());

        _dockLayoutState.Register(CreateShellPersistentDescriptor(ShellDockPanels.WorkspaceNavId, DockSide.Start), _ => { });
        _dockLayoutPersistence.ClearReceivedCalls();

        await cut.InvokeAsync(() => _dockLayoutState.Open(ShellDockPanels.WorkspaceNavId));
        await Task.Delay(TimeSpan.FromMilliseconds(650));

        await _dockLayoutPersistence.DidNotReceive().SaveAsync(
            Arg.Any<DockLayoutSnapshot>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NonPersistentPreviewChange_AfterWorkspaceHydration_DoesNotAutosaveEventsLayout()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));

        var cut = _ctx.RenderMudComponent<EventList>();

        cut.WaitForAssertion(() =>
            _dockLayoutPersistence.Received(1).LoadAsync("events", Arg.Any<CancellationToken>()).GetAwaiter().GetResult());

        _dockLayoutPersistence.ClearReceivedCalls();

        await cut.InvokeAsync(() => _dockLayoutState.Open(EventDockPanels.EventPreviewId));
        await Task.Delay(TimeSpan.FromMilliseconds(650));

        await _dockLayoutPersistence.DidNotReceive().SaveAsync(
            Arg.Any<DockLayoutSnapshot>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResetWorkspaceDockLayout_ClearsPersistentCustomizeStateAndDeletesEventsSnapshot()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        _dockLayoutPersistence.LoadAsync("events", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DockLayoutSnapshot?>(CreateWorkspaceSnapshot(customizeOpen: true, customizeWidth: 360)));

        var cut = _ctx.RenderMudComponent<EventList>();

        cut.WaitForAssertion(() =>
        {
            var customizePanel = _dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId);
            if (customizePanel?.State is not { IsOpen: true, Width: 360 })
                throw new InvalidOperationException("Expected workspace snapshot to restore Customize View before reset.");
        });

        SetPrivateField(cut.Instance, "_customizationDrawerOpen", true);
        _dockLayoutPersistence.ClearReceivedCalls();

        await InvokePrivateTaskAsync(cut, "ResetWorkspaceDockLayoutAsync");

        var customizeState = _dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId)?.State;
        var previewState = _dockLayoutState.GetPanel(EventDockPanels.EventPreviewId)?.State;

        await Assert.That(GetPrivateField<bool>(cut.Instance, "_customizationDrawerOpen")).IsFalse();
        await Assert.That(customizeState?.IsOpen).IsFalse();
        await Assert.That(customizeState?.Mode).IsEqualTo(DockMode.Docked);
        await Assert.That(customizeState?.Width).IsEqualTo(EventDockPanels.CustomizeView.DefaultWidth);
        await Assert.That(previewState?.IsOpen).IsFalse();

        await _dockLayoutPersistence.Received(1).DeleteAsync("events", Arg.Any<CancellationToken>());
        await _dockLayoutPersistence.DidNotReceive().SaveAsync(
            Arg.Any<DockLayoutSnapshot>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Render_WithShellEventSections_ShowsCuratedFilterLinks()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        _publicExperienceService.GetCachedShellAsync().Returns(Task.FromResult<PublicExperienceShellDto?>(new PublicExperienceShellDto
        {
            EventCatalog = new PublicExperienceEventCatalogDto { Label = "Programs", Url = "/events" },
            EventSections =
            [
                new PublicExperienceEventSectionDto
                {
                    Key = "youth",
                    Label = "Youth Programs",
                    Url = "/events?AudienceAgeIds=2",
                    Icon = "youth",
                    SortOrder = 20
                },
                new PublicExperienceEventSectionDto
                {
                    Key = "education",
                    Label = "Education",
                    Url = "/events?IncludedCategoryIds=11111111-1111-1111-1111-111111111111",
                    Icon = "education",
                    SortOrder = 10
                },
                new PublicExperienceEventSectionDto
                {
                    Key = "hidden",
                    Label = "",
                    Url = "/events?hidden=true",
                    SortOrder = 0
                }
            ]
        }));

        var cut = _ctx.RenderMudComponent<EventList>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup).Contains("Featured Programs");
            Assert.That(cut.Markup).Contains("Education");
            Assert.That(cut.Markup).Contains("Youth Programs");
            Assert.That(cut.Markup).Contains("Show Education");
            Assert.That(cut.Markup).DoesNotContain("hidden=true");
        });

        await Assert.That(cut.Markup.IndexOf("Education", StringComparison.Ordinal))
            .IsLessThan(cut.Markup.IndexOf("Youth Programs", StringComparison.Ordinal));
    }

    [Test]
    public async Task OpeningCustomizationDrawer_RendersCustomizeViewThroughWorkspaceDock()
    {
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokePrivateVoidAsync(cut, "OpenCustomizationDrawer");

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("[data-testid='dock-panel-host'][data-dock-panel-id='events.customize-view']").Count).IsEqualTo(1);
            Assert.That(cut.Markup).Contains("Customize View");
        });

        var panel = _dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId);
        await Assert.That(panel?.State.IsOpen).IsTrue();
        await Assert.That(panel?.State.Mode).IsEqualTo(DockMode.Docked);
    }

    [Test]
    public async Task DoesNotShowEmptyState_BeforeFirstEventsLoadCompletes()
    {
        // Arrange — result stays pending
        var pendingResult = new TaskCompletionSource<PaginatedResult<EventListDto>>();
        SetupPagedResult(pendingResult.Task);

        // Act
        var cut = _ctx.RenderMudComponent<EventList>();

        // Assert — empty state must not appear while load is pending
        await Assert.That(cut.Markup).DoesNotContain("No events found");

        // Cleanup to avoid dangling async work
        pendingResult.TrySetResult(CreateResult(1, 20, []));
    }

    [Test]
    public async Task ShowsNoEventsState_OnlyAfterInitialLoadCompletesWithEmptyResult()
    {
        // Arrange — start with pending result
        var pendingResult = new TaskCompletionSource<PaginatedResult<EventListDto>>();
        SetupPagedResult(pendingResult.Task);

        var cut = _ctx.RenderMudComponent<EventList>();

        // Pre-condition: while pending, empty state is not shown
        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup).DoesNotContain("No events found");
        });

        // Complete with empty result and trigger the Virtualize provider
        pendingResult.SetResult(CreateResult(1, 20, []));
        await InvokeLoadEventsAsync(cut);

        // Assert — empty state now visible
        await Assert.That(cut.Markup).Contains("No events found");
    }

    [Test]
    public async Task ShowsNoMatchesState_WhenFilteredResultIsEmpty()
    {
        var actorId = Guid.NewGuid();
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/events?actorId={actorId}");

        var cut = _ctx.RenderMudComponent<EventList>();
        await InvokeLoadEventsAsync(cut);

        await Assert.That(cut.Markup).Contains("No matching events found");
        await Assert.That(cut.Markup).Contains("Try adjusting your filters or search query.");
    }

    [Test]
    public async Task HidesNoEventsState_WhenResultsExist()
    {
        // Arrange — immediate result with one event
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Blazor Summit", Description = "Event description" }
        };
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, events)));

        // Act
        var cut = _ctx.RenderMudComponent<EventList>();
        await InvokeLoadEventsAsync(cut);

        // Assert — event visible, empty state hidden
        await Assert.That(cut.Markup).Contains("Blazor Summit");
        await Assert.That(cut.Markup).DoesNotContain("No events found");
    }

    [Test]
    public async Task SelectingEvent_PreservesCustomizationDrawerAndOpensDetailDrawer()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokePrivateVoidAsync(cut, "OpenCustomizationDrawer");
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_customizationDrawerOpen")).IsTrue();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId)?.State.IsOpen).IsTrue();

        await InvokePrivateTaskAsync(cut, "SelectEvent", new EventListDto
        {
            Id = eventId,
            Title = "Dock Baseline Event"
        });

        await Assert.That(GetPrivateField<bool>(cut.Instance, "_customizationDrawerOpen")).IsTrue();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId)?.State.IsOpen).IsTrue();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_detailDrawerOpen")).IsTrue();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.EventPreviewId)?.State.IsOpen).IsTrue();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll("[data-testid='dock-panel-host'][data-dock-panel-id='events.event-preview'][data-dock-mode='inspector']").Count).IsEqualTo(1);
            Assert.That(cut.FindAll("[data-testid='dock-resize-handle'][data-dock-resize-panel-id='events.event-preview']").Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task SelectingEvent_WithBlankFeaturedImage_DisplaysFallbackAfterDetailLoads()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokePrivateTaskAsync(cut, "SelectEvent", new EventListDto
        {
            Id = eventId,
            Title = "Fallback Preview Event",
            FeaturedImageUri = "   "
        });

        var image = cut.Find("img.event-list__detail-image-fallback");

        await Assert.That(cut.FindAll(".event-list__detail-image-skeleton")).IsEmpty();
        await Assert.That(image.GetAttribute("class")).DoesNotContain("event-list__detail-image--loading");
        await Assert.That(image.GetAttribute("src")).StartsWith("data:image/svg+xml;utf8,");
        await Assert.That(cut.FindAll("img.event-list__detail-image-actual")).IsEmpty();
    }

    [Test]
    public async Task DetailImageLoaded_ClearsSidebarImageSkeleton()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokePrivateTaskAsync(cut, "SelectEvent", new EventListDto
        {
            Id = eventId,
            Title = "Image Preview Event",
            FeaturedImageUri = "https://cdn.example.test/event.jpg"
        });

        var loadingImage = cut.Find("img.event-list__detail-image-actual");
        await Assert.That(cut.FindAll(".event-list__detail-image-skeleton")).Count().IsEqualTo(1);
        await Assert.That(loadingImage.GetAttribute("class")).Contains("event-list__detail-image--loading");

        await InvokePrivateTaskAsync(cut, "HandleDetailImageLoaded");

        var loadedImage = cut.Find("img.event-list__detail-image-actual");
        await Assert.That(cut.FindAll(".event-list__detail-image-skeleton")).IsEmpty();
        await Assert.That(loadedImage.GetAttribute("class")).DoesNotContain("event-list__detail-image--loading");
    }

    [Test]
    public async Task OpeningCustomizationDrawer_PreservesOpenDetailDrawer()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokePrivateTaskAsync(cut, "SelectEvent", new EventListDto
        {
            Id = eventId,
            Title = "Dock Baseline Event"
        });

        await Assert.That(GetPrivateField<bool>(cut.Instance, "_detailDrawerOpen")).IsTrue();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.EventPreviewId)?.State.IsOpen).IsTrue();

        await InvokePrivateVoidAsync(cut, "OpenCustomizationDrawer");

        await Assert.That(GetPrivateField<bool>(cut.Instance, "_detailDrawerOpen")).IsTrue();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_customizationDrawerOpen")).IsTrue();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.EventPreviewId)?.State.IsOpen).IsTrue();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.CustomizeViewId)?.State.IsOpen).IsTrue();
    }

    [Test]
    public async Task ClosingDetailDrawer_ResetsDetailPanelTransientState()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokePrivateTaskAsync(cut, "SelectEvent", new EventListDto
        {
            Id = eventId,
            Title = "Dock Baseline Event"
        });

        SetPrivateField(cut.Instance, "_showInlineRegistration", true);
        SetPrivateField(cut.Instance, "_showTagCatPopup", true);

        await InvokePrivateTaskAsync(cut, "CloseDetailDrawer");

        await Assert.That(GetPrivateField<bool>(cut.Instance, "_detailDrawerOpen")).IsFalse();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.EventPreviewId)?.State.IsOpen).IsFalse();
        await Assert.That(GetPrivateField<EventDto?>(cut.Instance, "_selectedEventDetail")).IsNull();
        await Assert.That(GetPrivateField<ICollection<EventSessionListDto>?>(cut.Instance, "_selectedEventSessions")).IsNull();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_showInlineRegistration")).IsFalse();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_showTagCatPopup")).IsFalse();
    }

    [Test]
    public async Task BackdropClosingEventPreview_SynchronizesDetailPreviewState()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await InvokePrivateTaskAsync(cut, "SelectEvent", new EventListDto
        {
            Id = eventId,
            Title = "Dock Backdrop Event"
        });

        SetPrivateField(cut.Instance, "_showInlineRegistration", true);
        SetPrivateField(cut.Instance, "_showTagCatPopup", true);
        await cut.Find("[data-testid='dock-overlay-backdrop']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.That(GetPrivateField<bool>(cut.Instance, "_detailDrawerOpen")).IsFalse();
            Assert.That(GetPrivateField<bool>(cut.Instance, "_showInlineRegistration")).IsFalse();
            Assert.That(GetPrivateField<bool>(cut.Instance, "_showTagCatPopup")).IsFalse();
            Assert.That(_dockLayoutState.GetPanel(EventDockPanels.EventPreviewId)?.State.IsOpen).IsFalse();
        });
    }

    [Test]
    public async Task NavigatingEventPreview_ResetsTransientPopupStateBeforeSelectingNextEvent()
    {
        var firstEventId = Guid.NewGuid();
        var nextEventId = Guid.NewGuid();
        var events = new List<EventListDto>
        {
            new() { Id = firstEventId, Title = "First Preview Event" },
            new() { Id = nextEventId, Title = "Next Preview Event" }
        };

        SetupPagedResult(Task.FromResult(CreateResult(1, 20, events)));
        SetupEventDetailResponse(firstEventId, "First Preview Event");
        SetupEventDetailResponse(nextEventId, "Next Preview Event");

        var cut = _ctx.RenderMudComponent<EventList>();
        await InvokeLoadEventsAsync(cut);
        await InvokePrivateTaskAsync(cut, "SelectEvent", events[0]);

        SetPrivateField(cut.Instance, "_showInlineRegistration", true);
        SetPrivateField(cut.Instance, "_showTagCatPopup", true);

        await InvokePrivateTaskAsync(cut, "NavigateNextEvent");

        await Assert.That(GetPrivateField<EventListDto?>(cut.Instance, "_selectedEvent")?.Id).IsEqualTo(nextEventId);
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_detailDrawerOpen")).IsTrue();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_showInlineRegistration")).IsFalse();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_showTagCatPopup")).IsFalse();
        await Assert.That(_dockLayoutState.GetPanel(EventDockPanels.EventPreviewId)?.State.IsOpen).IsTrue();
    }

    [Test]
    public async Task InlineRegistrationSuccess_RendersThreeActionChoices()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await RenderInlineRegistrationStateAsync(cut, new EventListDto
        {
            Id = eventId,
            Title = "Shareable Event"
        }, "_regIsComplete");

        await Assert.That(cut.Markup).Contains("Add to Calendar");
        await Assert.That(cut.Markup).Contains($"href=\"/api/event/{eventId}/calendar\"");
        await Assert.That(cut.Markup).Contains("Share this Event");
        await Assert.That(cut.Markup).Contains("View Registrations");
        await Assert.That(cut.Markup).Contains("href=\"/my/profile\"");
    }

    [Test]
    public async Task InlineRegistrationWaitlist_RendersWaitlistFeedbackAndFollowUpActions()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await RenderInlineRegistrationStateAsync(cut, new EventListDto
        {
            Id = eventId,
            Title = "Full Event"
        }, "_regIsComplete", isWaitlisted: true);

        await Assert.That(cut.Markup).Contains("You're on the Waitlist!");
        await Assert.That(cut.Markup).Contains("You have been added to the waitlist");
        await Assert.That(cut.Markup).Contains("Share this Event");
        await Assert.That(cut.Markup).Contains("View Registrations");
    }

    [Test]
    public async Task InlineAlreadyRegisteredState_RendersShareAndRegistrationActions()
    {
        var eventId = Guid.NewGuid();
        SetupEventDetailResponses(eventId);

        var cut = _ctx.RenderMudComponent<EventList>();

        await RenderInlineRegistrationStateAsync(cut, new EventListDto
        {
            Id = eventId,
            Title = "Shareable Event"
        }, "_regIsAlreadyRegistered");

        await Assert.That(cut.Markup).Contains("Already Registered");
        await Assert.That(cut.Markup).Contains("Share this Event");
        await Assert.That(cut.Markup).Contains("View Registrations");
        await Assert.That(cut.Markup).Contains("href=\"/my/profile\"");
    }

    [Test]
    public async Task InlineRegistrationSubmit_UsesRegistrationServiceWithConsentSnapshot()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        _registrationService.RegisterForSessionAsync(Arg.Any<CreateEventRegistrationDto>())
            .Returns(Task.FromResult<BaseCommandResponseOfGuid?>(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Event Registration created successfully."
            }));

        var cut = _ctx.RenderMudComponent<EventList>();
        SetPrivateField(cut.Instance, "_selectedEvent", new EventListDto
        {
            Id = eventId,
            Title = "Registration Service Event"
        });
        SetPrivateField(cut.Instance, "_regCurrentUser", new UserDto
        {
            Id = userId,
            Email = "registrant@example.com",
            FirstName = "Test",
            LastName = "Registrant"
        });
        SetPrivateField(cut.Instance, "_regSelectedSessionIds", new HashSet<Guid> { sessionId });
        SetPrivateField(cut.Instance, "_regShareEmail", true);
        SetPrivateField(cut.Instance, "_regOrganizerName", "Community Organizer");

        await InvokePrivateTaskAsync(cut, "HandleInlineRegistrationSubmit");

        await _registrationService.Received(1).RegisterForSessionAsync(Arg.Is<CreateEventRegistrationDto>(dto =>
            dto != null
            && dto.EventId == eventId
            && dto.UserId == userId
            && dto.RegistrationScopeId == 3
            && dto.SelectedSessionIds != null
            && dto.SelectedSessionIds.SequenceEqual(new[] { sessionId })
            && dto.ShareEmailWithOrganizer == true
            && dto.ConsentTextAcknowledged != null
            && dto.ConsentTextAcknowledged.Contains("Community Organizer", StringComparison.Ordinal)
            && dto.ConsentUiVersion == "v1"));
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_regIsComplete")).IsTrue();
    }

    [Test]
    public async Task InlineRegistrationSubmit_WhenWholeEventPolicyAndAllSessionsSelected_UsesEventScope()
    {
        var eventId = Guid.NewGuid();
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        _registrationService.RegisterForSessionAsync(Arg.Any<CreateEventRegistrationDto>())
            .Returns(Task.FromResult<BaseCommandResponseOfGuid?>(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Event Registration created successfully."
            }));

        var cut = _ctx.RenderMudComponent<EventList>();
        SetPrivateField(cut.Instance, "_selectedEvent", new EventListDto
        {
            Id = eventId,
            Title = "Whole Event Registration"
        });
        SetPrivateField(cut.Instance, "_selectedEventDetail", new EventDto
        {
            Id = eventId,
            Title = "Whole Event Registration",
            Description = "Event that only permits whole-event registration.",
            RegistrationPolicyId = 1
        });
        SetPrivateField<ICollection<EventSessionListDto>>(cut.Instance, "_regAvailableSessions",
        [
            new EventSessionListDto { Id = firstSessionId },
            new EventSessionListDto { Id = secondSessionId }
        ]);
        SetPrivateField(cut.Instance, "_regCurrentUser", new UserDto
        {
            Id = userId,
            Email = "registrant@example.com",
            FirstName = "Test",
            LastName = "Registrant"
        });
        SetPrivateField(cut.Instance, "_regSelectedSessionIds", new HashSet<Guid> { firstSessionId, secondSessionId });
        SetPrivateField(cut.Instance, "_regShareEmail", true);
        SetPrivateField(cut.Instance, "_regOrganizerName", "Community Organizer");

        await InvokePrivateTaskAsync(cut, "HandleInlineRegistrationSubmit");

        await _registrationService.Received(1).RegisterForSessionAsync(Arg.Is<CreateEventRegistrationDto>(dto =>
            dto != null
            && dto.EventId == eventId
            && dto.UserId == userId
            && dto.RegistrationScopeId == 1
            && dto.SelectedSessionIds == null
            && dto.SelectedEventDayId == null
            && dto.ShareEmailWithOrganizer == true
            && dto.ConsentTextAcknowledged != null
            && dto.ConsentTextAcknowledged.Contains("Community Organizer", StringComparison.Ordinal)
            && dto.ConsentUiVersion == "v1"));
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_regIsComplete")).IsTrue();
    }

    [Test]
    public async Task InlineRegistrationSubmit_WhenApiReturnsAlreadyExists_ShowsAlreadyRegisteredState()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var snackbar = _ctx.Services.GetRequiredService<ISnackbar>();
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        _registrationService.RegisterForSessionAsync(Arg.Any<CreateEventRegistrationDto>())
            .Returns(Task.FromResult<BaseCommandResponseOfGuid?>(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Event Registration already exists."
            }));

        var cut = _ctx.RenderMudComponent<EventList>();
        SetPrivateField(cut.Instance, "_selectedEvent", new EventListDto
        {
            Id = eventId,
            Title = "Repeat Registration Event"
        });
        SetPrivateField(cut.Instance, "_regCurrentUser", new UserDto
        {
            Id = userId,
            Email = "registrant@example.com",
            FirstName = "Test",
            LastName = "Registrant"
        });
        SetPrivateField(cut.Instance, "_regSelectedSessionIds", new HashSet<Guid> { sessionId });

        await InvokePrivateTaskAsync(cut, "HandleInlineRegistrationSubmit");

        await Assert.That(GetPrivateField<bool>(cut.Instance, "_regIsAlreadyRegistered")).IsTrue();
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_regIsComplete")).IsFalse();
        snackbar.Received().Add("You are already registered for this event.", Severity.Info);
    }

    [Test]
    public async Task InlineRegistrationSubmit_WhenRegistrationThrows_ShowsGenericErrorOnly()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var snackbar = _ctx.Services.GetRequiredService<ISnackbar>();
        SetupPagedResult(Task.FromResult(CreateResult(1, 20, [])));
        _registrationService.RegisterForSessionAsync(Arg.Any<CreateEventRegistrationDto>())
            .Returns(_ => Task.FromException<BaseCommandResponseOfGuid?>(new InvalidOperationException("database exploded")));

        var cut = _ctx.RenderMudComponent<EventList>();
        SetPrivateField(cut.Instance, "_selectedEvent", new EventListDto
        {
            Id = eventId,
            Title = "Registration Error Event"
        });
        SetPrivateField(cut.Instance, "_regCurrentUser", new UserDto
        {
            Id = userId,
            Email = "registrant@example.com",
            FirstName = "Test",
            LastName = "Registrant"
        });
        SetPrivateField(cut.Instance, "_regSelectedSessionIds", new HashSet<Guid> { sessionId });

        await InvokePrivateTaskAsync(cut, "HandleInlineRegistrationSubmit");

        snackbar.Received().Add("Registration failed. Please try again.", Severity.Error);
        snackbar.DidNotReceive().Add(Arg.Is<string>(message => message.Contains("database exploded", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }
}
