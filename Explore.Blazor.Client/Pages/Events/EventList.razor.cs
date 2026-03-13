// ABOUTME: Event list page logic for loading filters, events, and registrations.
// ABOUTME: Preserves initial prerender results to avoid hydration flicker on SEO pages.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Dialogs;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

public partial class EventList : ComponentBase, IAsyncDisposable
{
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected ICategoryService CategoryService { get; set; } = null!;
    [Inject] protected ITagService TagService { get; set; } = null!;
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected ILocationService LocationService { get; set; } = null!;
    [Inject] protected IEventRegistrationService RegistrationService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected IPublicExperienceService PublicExperienceService { get; set; } = null!;
    [Inject] protected ILogger<EventList> Logger { get; set; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    [PersistentState]
    public EventListState? PersistedState { get; set; }

    private EventFilterBar? _filterBar;
    private string? _errorMessage;
    private string? _successMessage;
    private string? _searchTerm;
    private bool _filtersOpen;
    private LayoutMode _currentLayout = LayoutMode.DetailedList;

    // Module Flags
    private bool _isIslamicModuleEnabled;
    private bool _isTechModuleEnabled;
    private bool _eventCardClickOpensDetailPage;

    // Detail drawer (right sidebar) state
    private bool _detailDrawerOpen;
    private EventListDto? _selectedEvent;
    private EventDto? _selectedEventDetail;
    private ICollection<EventSessionListDto>? _selectedEventSessions;
    private bool _isLoadingDetail;

    private bool isLoading = true;
    private bool _eventsLoaded = false;
    private bool _dataLoaded = false;
    private bool _usePersistedEvents = false;
    private bool _virtualizeRefreshed = false;
    private bool _useInitialBatch = false;
    private PaginatedResult<EventListDto>? _initialBatch;

    private Virtualize<EventListDto>? _virtualize;
    private int _totalCount;
    private IJSObjectReference? _imagePreloaderModule;

    // API Data
    private ICollection<EventTypeListDto> eventTypes = new List<EventTypeListDto>();
    private ICollection<AudienceGenderListDto> audienceGenders = new List<AudienceGenderListDto>();
    private ICollection<AudienceAgeListDto> audienceAges = new List<AudienceAgeListDto>();
    private ICollection<EventStatusListDto> eventStatuses = new List<EventStatusListDto>();
    private ICollection<EventFormatListDto> eventFormats = new List<EventFormatListDto>();
    private ICollection<CategoryListDto> categories = new List<CategoryListDto>();
    private ICollection<TagListDto> tags = new List<TagListDto>();
    private ICollection<MadhabListDto> madhabs = new List<MadhabListDto>();
    private ICollection<LocationListDto> locations = new List<LocationListDto>();
    private ICollection<RegistrationModeListDto> registrationModes = new List<RegistrationModeListDto>();
    private ICollection<LanguageListDto> languages = new List<LanguageListDto>();
    private ICollection<TagTypeWithTagsDto> tagGroups = new List<TagTypeWithTagsDto>();
    private ICollection<CategoryTypeWithCategoriesDto> categoryGroups = new List<CategoryTypeWithCategoriesDto>();

    private Dictionary<int, string> eventTypeMap = new();
    private Dictionary<int, string> eventFormatMap = new();

    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IContactShareConsentService ConsentService { get; set; } = default!;

    private HashSet<Guid> _registeredEventIds = new();
    private Dictionary<Guid, Guid> _registrationIdByEventId = new();
    private bool _isCancellingRegistration = false;

    // Inline registration state
    private bool _showInlineRegistration;
    private bool _regIsLoading;
    private bool _regIsSubmitting;
    private bool _regIsComplete;
    private bool _regIsAlreadyRegistered;
    private bool _regShowConsentOption;
    private bool _regShareEmail;
    private string _regOrganizerName = "";
    private UserDto? _regCurrentUser;
    private ICollection<EventSessionListDto>? _regAvailableSessions;
    private HashSet<Guid> _regSelectedSessionIds = new();
    private bool _regAllSessionsSelected => _regAvailableSessions != null
        && _regAvailableSessions.Any(s => s.Id.HasValue)
        && _regSelectedSessionIds.Count == _regAvailableSessions.Count(s => s.Id.HasValue);

    // Event navigation cache (for prev/next arrows)
    private List<EventListDto> _loadedEvents = new();

    // Tag/Category management popup state
    private bool _showTagCatPopup;
    private TagCategoryMode _tagCatMode;
    private IReadOnlyCollection<Guid> _tagCatInitialIds = Array.Empty<Guid>();

    [SupplyParameterFromQuery(Name = "q")]
    public string? SearchQuery { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogDebug("OnInitializedAsync starting");

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            // Defer search query set until filter bar is ready or handle in LoadEvents
        }

        if (TryRestoreState())
        {
            return;
        }

        var settings = await PublicExperienceService.GetSettingsAsync();
        if (settings != null)
        {
            _isIslamicModuleEnabled = settings.IsIslamicModuleEnabled;
            _isTechModuleEnabled = settings.IsTechModuleEnabled;
            _eventCardClickOpensDetailPage = settings.EventCardClickOpensDetailPage;
        }

        await LoadDataAsync();
        await PreloadInitialEventsAsync();
        await LoadUserRegistrationsAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Logger.LogWarning("OnAfterRenderAsync: firstRender={First}, _dataLoaded={Data}, _virtualize={Virt}, _virtualizeRefreshed={Refreshed}, _eventsLoaded={Events}",
            firstRender, _dataLoaded, _virtualize != null, _virtualizeRefreshed, _eventsLoaded);

        // Virtualize's IntersectionObserver may not fire when it first appears
        // in a conditional render block inside MudGrid. Force the initial load.
        if (_dataLoaded && _virtualize != null && !_virtualizeRefreshed)
        {
            _virtualizeRefreshed = true;
            await _virtualize.RefreshDataAsync();
        }
    }

    private bool TryRestoreState()
    {
        if (PersistedState == null)
        {
            return false;
        }

        _isIslamicModuleEnabled = PersistedState.IsIslamicModuleEnabled;
        _isTechModuleEnabled = PersistedState.IsTechModuleEnabled;
        _eventCardClickOpensDetailPage = PersistedState.EventCardClickOpensDetailPage;

        eventTypes = PersistedState.EventTypes;
        audienceGenders = PersistedState.AudienceGenders;
        audienceAges = PersistedState.AudienceAges;
        eventStatuses = PersistedState.EventStatuses;
        eventFormats = PersistedState.EventFormats;
        categories = PersistedState.Categories;
        tags = PersistedState.Tags;
        madhabs = PersistedState.Madhabs;
        locations = PersistedState.Locations;
        registrationModes = PersistedState.RegistrationModes;
        languages = PersistedState.Languages;
        tagGroups = PersistedState.TagGroups;
        categoryGroups = PersistedState.CategoryGroups;

        BuildLookupMaps();
        _dataLoaded = true;
        _totalCount = PersistedState.TotalCount;
        _eventsLoaded = true;
        isLoading = false;
        _usePersistedEvents = true;

        _ = InvokeAsync(async () =>
        {
            await LoadUserRegistrationsAsync();
            StateHasChanged();
        });

        return true;
    }

    private async Task LoadUserRegistrationsAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                var user = await UserService.GetCurrentUserAsync();
                if (user != null && user.Id.HasValue)
                {
                    var registrations = await EventService.GetRegistrationsByUserAsync(user.Id.Value);
                    if (registrations != null)
                    {
                        var eventIds = new HashSet<Guid>();
                        var regMap = new Dictionary<Guid, Guid>();
                        foreach (var reg in registrations)
                        {
                            if (reg.EventId.HasValue && reg.Id.HasValue)
                            {
                                eventIds.Add(reg.EventId.Value);
                                regMap[reg.EventId.Value] = reg.Id.Value;
                            }
                        }
                        _registeredEventIds = eventIds;
                        _registrationIdByEventId = regMap;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load user registrations");
        }
    }

    private bool IsUserRegistered(Guid eventId)
    {
        return _registeredEventIds.Contains(eventId);
    }

    private void BuildLookupMaps()
    {
        eventTypeMap = eventTypes.Where(et => et.Id.HasValue).ToDictionary(et => et.Id.Value, et => et.FullName);
        eventFormatMap = eventFormats.Where(pt => pt.Id.HasValue).ToDictionary(pt => pt.Id.Value, pt => pt.FullName);
    }

    private async Task LoadDataAsync()
    {
        if (_dataLoaded) return;
        isLoading = true;
        try
        {
            var eventTypesTask = AdminService.GetEventTypesAsync();
            var audienceGendersTask = AdminService.GetAudienceGendersAsync();
            var audienceAgesTask = AdminService.GetAudienceAgesAsync();
            var eventStatusesTask = AdminService.GetEventStatusesAsync();
            var eventFormatsTask = EventService.GetEventFormatsAsync();
            var categoriesTask = CategoryService.GetAllCategoriesAsync();
            var tagsTask = TagService.GetAllTagsAsync();
            var madhabsTask = AdminService.GetMadhabsAsync();
            var locationsTask = LocationService.GetAllLocationsAsync();
            var registrationModesTask = AdminService.GetRegistrationModesAsync();
            var languagesTask = AdminService.GetLanguagesAsync();
            var tagGroupsTask = TagService.GetTagsGroupedByTagTypeAsync();
            var categoryGroupsTask = CategoryService.GetCategoriesGroupedByCategoryTypeAsync();

            await Task.WhenAll(eventTypesTask, audienceGendersTask, audienceAgesTask, eventStatusesTask, eventFormatsTask, categoriesTask, tagsTask, madhabsTask, locationsTask, registrationModesTask, languagesTask, tagGroupsTask, categoryGroupsTask);

            eventTypes = await eventTypesTask ?? new List<EventTypeListDto>();
            audienceGenders = await audienceGendersTask ?? new List<AudienceGenderListDto>();
            audienceAges = await audienceAgesTask ?? new List<AudienceAgeListDto>();
            eventStatuses = await eventStatusesTask ?? new List<EventStatusListDto>();
            eventFormats = await eventFormatsTask ?? new List<EventFormatListDto>();
            categories = await categoriesTask ?? new List<CategoryListDto>();
            tags = await tagsTask ?? new List<TagListDto>();
            madhabs = await madhabsTask ?? new List<MadhabListDto>();
            locations = await locationsTask ?? new List<LocationListDto>();
            registrationModes = await registrationModesTask ?? new List<RegistrationModeListDto>();
            languages = await languagesTask ?? new List<LanguageListDto>();
            tagGroups = await tagGroupsTask ?? new List<TagTypeWithTagsDto>();
            categoryGroups = await categoryGroupsTask ?? new List<CategoryTypeWithCategoriesDto>();

            // When junction tables (CategoryTypeCategories / TagTypeTags) are empty,
            // the grouped endpoints return nothing even though categories/tags exist.
            // Fall back to a single "All" group so the TriState filter dropdowns still work.
            if (!categoryGroups.Any() && categories.Any())
            {
                categoryGroups = new List<CategoryTypeWithCategoriesDto>
                {
                    new() { Id = 0, FullName = "All Categories", Categories = categories.ToList() }
                };
                Logger.LogDebug("CategoryGroups empty; created fallback group with {Count} categories", categories.Count);
            }

            if (!tagGroups.Any() && tags.Any())
            {
                tagGroups = new List<TagTypeWithTagsDto>
                {
                    new() { Id = 0, FullName = "All Tags", Tags = tags.ToList() }
                };
                Logger.LogDebug("TagGroups empty; created fallback group with {Count} tags", tags.Count);
            }

            BuildLookupMaps();
            _dataLoaded = true;
            // Don't set isLoading = false here. Wait for the first batch of events.
            // isLoading = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadDataAsync error");
            isLoading = false; // Stop loading on error
        }
    }

    private async Task PreloadInitialEventsAsync()
    {
        if (!_dataLoaded) return;

        try
        {
            _initialBatch = await EventService.GetEventsPagedAsync(
                pageNumber: 1,
                pageSize: 20,
                cancellationToken: CancellationToken.None);

            _totalCount = _initialBatch.TotalCount;
            _useInitialBatch = true;

            // Preload images into the browser cache so cards appear with images ready
            if (_initialBatch.Items.Any())
            {
                var imageUrls = _initialBatch.Items
                    .Select(evt => string.IsNullOrEmpty(evt.FeaturedImageUri) ? GetEventImage(evt) : evt.FeaturedImageUri!)
                    .ToArray();

                try
                {
                    _imagePreloaderModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>(
                        "import", "./js/image-preloader.js");
                    await _imagePreloaderModule.InvokeVoidAsync("preloadImages", (object)imageUrls);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Image preloading failed, proceeding without it");
                }
            }

            _eventsLoaded = true;
            isLoading = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PreloadInitialEventsAsync error");
            // Still flip to loaded state so the page isn't stuck on skeleton
            _eventsLoaded = true;
            isLoading = false;
        }
    }

    private async ValueTask<ItemsProviderResult<EventListDto>> LoadEventsAsync(ItemsProviderRequest request)
    {
        Logger.LogWarning("LoadEventsAsync called: StartIndex={Start}, Count={Count}, _usePersistedEvents={Persisted}, _useInitialBatch={Batch}",
            request.StartIndex, request.Count, _usePersistedEvents, _useInitialBatch);

        if (_usePersistedEvents && PersistedState != null && request.StartIndex == PersistedState.InitialStartIndex)
        {
            _usePersistedEvents = false;
            return new ItemsProviderResult<EventListDto>(PersistedState.InitialItems, PersistedState.TotalCount);
        }

        // Reuse the initial batch that was already fetched and image-preloaded
        if (_useInitialBatch && _initialBatch != null && request.StartIndex == 0)
        {
            _useInitialBatch = false;
            var batch = _initialBatch;
            _initialBatch = null;
            return new ItemsProviderResult<EventListDto>(batch.Items, batch.TotalCount);
        }

        var pageSize = Math.Max(request.Count, 20);
        var pageNumber = (request.StartIndex / pageSize) + 1;

        // Get filter values from _filterBar or defaults
        var searchTerm = _filterBar?.SearchTerm ?? SearchQuery;

        // Multi-select filter values — pass full lists to service
        var formatIds = _filterBar?.SelectedFormatIds?.ToList();
        var madhabIds = _filterBar?.SelectedMadhabIds?.ToList();
        var locationIds = _filterBar?.SelectedLocationIds?.ToList();
        var registrationModeIds = _filterBar?.SelectedRegistrationModeIds?.ToList();
        var languageIds = _filterBar?.SelectedLanguageIds?.ToList();
        var eventTypeIds = _filterBar?.SelectedEventTypeIds?.ToList();
        var audienceGenderIds = _filterBar?.SelectedAudienceGenderIds?.ToList();
        var audienceAgeIds = _filterBar?.SelectedAudienceAgeIds?.ToList();
        var eventStatusIds = _filterBar?.SelectedEventStatusIds?.ToList();

        var sortBy = _filterBar?.SelectedSortBy ?? "date";
        var sortDescending = _filterBar?.SortDescending ?? true;

        // Islamic
        var genderModeIds = _filterBar?.SelectedGenderModeIds?.ToList();
        var referencePrayerIds = _filterBar?.SelectedReferencePrayerIds?.ToList();

        // Tech
        var skillLevelId = _filterBar?.SelectedSkillLevel != null ? (int?)_filterBar.SelectedSkillLevel : null;
        var techStack = _filterBar?.TechStackTag;

        // Date range from MudDateRangePicker
        DateTimeOffset? dateFrom = null;
        DateTimeOffset? dateTo = null;
        if (_filterBar?.SelectedDateRange?.Start != null)
        {
            dateFrom = new DateTimeOffset(_filterBar.SelectedDateRange.Start.Value, TimeSpan.Zero);
        }
        if (_filterBar?.SelectedDateRange?.End != null)
        {
            dateTo = new DateTimeOffset(_filterBar.SelectedDateRange.End.Value.AddDays(1).AddTicks(-1), TimeSpan.Zero);
        }

        var result = await EventService.GetEventsPagedAsync(
            pageNumber,
            pageSize,
            searchTerm: searchTerm,
            includedCategoryIds: _filterBar?.GetCategoryFilter().IncludedCategoryIds,
            excludedCategoryIds: _filterBar?.GetCategoryFilter().ExcludedCategoryIds,
            categoryInclusionMode: _filterBar?.GetCategoryFilter().InclusionMode,
            categoryExclusionMode: _filterBar?.GetCategoryFilter().ExclusionMode,
            includedTagIds: _filterBar?.GetTagFilter().IncludedTagIds,
            excludedTagIds: _filterBar?.GetTagFilter().ExcludedTagIds,
            inclusionMode: _filterBar?.GetTagFilter().InclusionMode,
            exclusionMode: _filterBar?.GetTagFilter().ExclusionMode,
            formatIds: formatIds,
            madhabIds: madhabIds,
            locationIds: locationIds,
            registrationModeIds: registrationModeIds,
            languageIds: languageIds,
            dateFrom: dateFrom,
            dateTo: dateTo,
            sortBy: sortBy,
            sortDescending: sortDescending,
            eventTypeIds: eventTypeIds,
            audienceGenderIds: audienceGenderIds,
            audienceAgeIds: audienceAgeIds,
            eventStatusIds: eventStatusIds,
            genderModeIds: genderModeIds,
            includesQuranRecitation: null,
            referencePrayerIds: referencePrayerIds,
            islamicPrimaryLanguageIds: null,
            hasIslamicAspect: null,
            skillLevelId: skillLevelId,
            isCodingCompetition: null,
            isHackathon: null,
            requiresLaptop: null,
            techStackTag: techStack,
            hasTechAspect: null,
            cancellationToken: request.CancellationToken);

        _totalCount = result.TotalCount;
        _eventsLoaded = true;
        if (isLoading) isLoading = false;

        // Cache loaded events for prev/next navigation
        foreach (var evt in result.Items)
        {
            if (evt.Id.HasValue && !_loadedEvents.Any(e => e.Id == evt.Id))
                _loadedEvents.Add(evt);
        }

        StateHasChanged();

        if (PersistedState == null && request.StartIndex == 0)
        {
            PersistedState = new EventListState
            {
                InitialItems = result.Items.ToList(),
                TotalCount = result.TotalCount,
                InitialStartIndex = request.StartIndex,
                IsIslamicModuleEnabled = _isIslamicModuleEnabled,
                IsTechModuleEnabled = _isTechModuleEnabled,
                EventCardClickOpensDetailPage = _eventCardClickOpensDetailPage,
                EventTypes = eventTypes.ToList(),
                AudienceGenders = audienceGenders.ToList(),
                AudienceAges = audienceAges.ToList(),
                EventStatuses = eventStatuses.ToList(),
                EventFormats = eventFormats.ToList(),
                Categories = categories.ToList(),
                Tags = tags.ToList(),
                Madhabs = madhabs.ToList(),
                Locations = locations.ToList(),
                RegistrationModes = registrationModes.ToList(),
                Languages = languages.ToList(),
                TagGroups = tagGroups.ToList(),
                CategoryGroups = categoryGroups.ToList()
            };
        }

        return new ItemsProviderResult<EventListDto>(result.Items, result.TotalCount);
    }

    private async Task RefreshList()
    {
        if (_virtualize != null)
        {
            _loadedEvents.Clear();
            await _virtualize.RefreshDataAsync();
        }
    }

    private async Task SelectEvent(EventListDto evt)
    {
        if (_eventCardClickOpensDetailPage)
        {
            Navigation.NavigateTo($"/event/detail/{evt.Id}");
            return;
        }

        _selectedEvent = evt;
        _selectedEventDetail = null;
        _selectedEventSessions = null;
        _detailDrawerOpen = true;
        _isLoadingDetail = true;

        try
        {
            var detailTask = EventService.GetEventByIdAsync(evt.Id!.Value);
            var sessionsTask = EventService.GetSessionsByEventAsync(evt.Id!.Value);
            await Task.WhenAll(detailTask, sessionsTask);

            _selectedEventDetail = detailTask.Result;
            _selectedEventSessions = sessionsTask.Result;
        }
        catch
        {
            // Silently fail — the panel still shows basic info from EventListDto
        }
        finally
        {
            _isLoadingDetail = false;
            StateHasChanged();
        }
    }

    private void CloseDetailDrawer()
    {
        _detailDrawerOpen = false;
        _selectedEventDetail = null;
        _selectedEventSessions = null;
        _showInlineRegistration = false;
        _showTagCatPopup = false;
    }

    private void OnDetailDrawerOpenChanged(bool open)
    {
        _detailDrawerOpen = open;
        if (!open)
        {
            _selectedEventDetail = null;
            _selectedEventSessions = null;
            _showInlineRegistration = false;
            _showTagCatPopup = false;
        }
    }

    private void HandleOutsideDrawerClick()
    {
        if (_showInlineRegistration || _showTagCatPopup)
        {
            _showInlineRegistration = false;
            _showTagCatPopup = false;
        }
        else
        {
            CloseDetailDrawer();
        }
    }

    private void HandleDrawerCloseClick()
    {
        if (_showInlineRegistration || _showTagCatPopup)
        {
            _showInlineRegistration = false;
            _showTagCatPopup = false;
        }
        else
        {
            CloseDetailDrawer();
        }
    }

    private async Task CopyEventLinkAsync()
    {
        if (_selectedEvent?.Id == null) return;
        var url = Navigation.ToAbsoluteUri($"/event/detail/{_selectedEvent.Id}").ToString();
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", url);
        Snackbar.Add("Link copied to clipboard", Severity.Success, options => options.VisibleStateDuration = 2000);
    }

    private void OnLayoutModeChanged(LayoutMode mode)
    {
        _currentLayout = mode;
    }

    private bool HasActiveFilters()
    {
        if (!string.IsNullOrEmpty(SearchQuery)) return true;
        if (_filterBar == null) return false;
        return _filterBar.GetActiveFilterCount() > 0;
    }

    private string GetGridCssClass()
    {
        return $"mt-4 event-grid event-grid--{_currentLayout}";
    }

    private string GetCardCssClass(EventListDto evt)
    {
        var baseClass = $"event-card event-card--{_currentLayout}";
        if (_currentLayout == LayoutMode.CompactGrid)
            baseClass += " rounded-lg";
        else
            baseClass += " rounded-xl";

        if (_selectedEvent?.Id == evt.Id && _detailDrawerOpen)
            baseClass += " event-card--selected";

        return baseClass;
    }

    private (int Xs, int Sm, int Md, int Lg, int Xl) GetGridBreakpoints()
    {
        return _currentLayout switch
        {
            LayoutMode.CompactGrid => (6, 4, 3, 2, 2),
            LayoutMode.DetailedList => (12, 12, 6, 4, 4),
            LayoutMode.SingleRow => (12, 12, 12, 12, 12),
            _ => (6, 6, 4, 3, 3)
        };
    }

    private async Task ApplyCategoryFilter(CategoryListDto category)
    {
        if (_filterBar != null && category.FullName != null)
        {
            _filterBar.SearchTerm = category.FullName;
        }
        await RefreshList();
    }

    // ... (helper methods like GetSelectedCategoryName can remain or be used for display)

    private void NavigateToEdit(EventListDto evt)
    {
        if (evt.Id.HasValue)
            Navigation.NavigateTo($"/event/edit/{evt.Id.Value}");
    }

    private async Task OpenDeleteDialog(EventListDto evt)
    {
        var parameters = new DialogParameters { ["EventId"] = evt.Id, ["EventTitle"] = evt.Title };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DeleteEventDialog.ShowAsync(DialogService, "Delete Event", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled)
        {
            await _virtualize?.RefreshDataAsync()!;
        }
    }

    private async Task OpenQuickRegisterDialog(EventListDto evt)
    {
        if (!evt.Id.HasValue) return;
        var sessions = await EventService.GetSessionsByEventAsync(evt.Id.Value);
        if (sessions == null || !sessions.Any())
        {
            _errorMessage = "No sessions available for this event yet.";
            return;
        }
        var primarySession = sessions.First();
        var parameters = new DialogParameters { ["EventSessionId"] = primarySession.Id, ["Title"] = $"Register for {evt.Title}" };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<EventRegistration>("Register", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled)
        {
            _successMessage = "Successfully registered for event!";
            await LoadUserRegistrationsAsync();
        }
    }

    private async Task CancelRegistrationAsync(EventListDto evt)
    {
        if (!evt.Id.HasValue) return;
        var eventId = evt.Id.Value;

        if (!_registrationIdByEventId.TryGetValue(eventId, out var registrationId))
        {
            _errorMessage = "Registration not found.";
            return;
        }

        var confirm = await DialogService.ShowMessageBoxAsync(
            "Cancel Registration",
            $"Are you sure you want to cancel your registration for \"{evt.Title}\"?",
            yesText: "Cancel Registration",
            cancelText: "Keep Registration");

        if (confirm != true) return;

        _isCancellingRegistration = true;

        try
        {
            var success = await EventService.CancelEventRegistrationAsync(registrationId);
            if (success)
            {
                _registeredEventIds.Remove(eventId);
                _registrationIdByEventId.Remove(eventId);
                _successMessage = "Registration cancelled.";
            }
            else
            {
                _errorMessage = "Failed to cancel registration. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cancelling registration for event {EventId}", eventId);
            _errorMessage = "An error occurred while cancelling registration.";
        }
        finally
        {
            _isCancellingRegistration = false;
        }
    }

    private string GetEventTypeName(EventListDto eventItem)
    {
        if (!string.IsNullOrEmpty(eventItem.EventTypeFullName)) return eventItem.EventTypeFullName;
        if (eventItem.EventTypeId.HasValue && eventTypeMap.TryGetValue(eventItem.EventTypeId.Value, out var eventTypeName))
            return eventTypeName;
        return "Event";
    }

    private string GetLocationText(EventListDto eventItem)
    {
        if (eventItem.EventFormatId == 2) return "Online";
        if (!string.IsNullOrEmpty(eventItem.EventFormatFullName)) return eventItem.EventFormatFullName;
        if (eventItem.EventFormatId.HasValue && eventFormatMap.TryGetValue(eventItem.EventFormatId.Value, out var formatName))
            return formatName;
        return "Location TBD";
    }

    private string GetEventImage(EventListDto eventItem)
    {
        return ImageHelper.GetEventImageUrl(eventItem.FeaturedImageUri, eventItem.Title, GetEventColorForEvent(eventItem));
    }

    private string GetEventColorForEvent(EventListDto eventItem)
    {
        var color = EventColorHelper.GetColorByTypeId(eventItem.EventTypeId);
        return color != EventColorHelper.DefaultColor ? color : EventColorHelper.GetColorByHash(eventItem.Title);
    }

    private string GetTruncatedDescription(string? description)
    {
        return StringHelper.TruncateDescription(description);
    }

    private string GetActorInitials(string? displayName)
    {
        return DisplayHelper.GetInitials(displayName);
    }

    private static Color GetStatusColor(string? masterCode) => masterCode switch
    {
        "PUBLISHED" => Color.Success,
        "DRAFT" => Color.Default,
        "CANCELLED" => Color.Error,
        "COMPLETED" => Color.Info,
        "POSTPONED" => Color.Warning,
        _ => Color.Default
    };

    private bool HasDetailTags()
    {
        if (_selectedEventDetail?.AdditionalProperties == null) return false;
        return _selectedEventDetail.AdditionalProperties.TryGetValue("tags", out var val) && val != null;
    }

    private IEnumerable<string> GetDetailTags()
    {
        if (_selectedEventDetail?.AdditionalProperties == null) yield break;
        if (!_selectedEventDetail.AdditionalProperties.TryGetValue("tags", out var val) || val is not System.Text.Json.JsonElement jsonArray) yield break;
        if (jsonArray.ValueKind != System.Text.Json.JsonValueKind.Array) yield break;
        foreach (var item in jsonArray.EnumerateArray())
        {
            var name = item.TryGetProperty("fullName", out var fn) ? fn.GetString()
                     : item.TryGetProperty("name", out var n) ? n.GetString()
                     : null;
            if (!string.IsNullOrEmpty(name)) yield return name;
        }
    }

    private bool HasDetailCategories()
    {
        if (_selectedEventDetail?.AdditionalProperties == null) return false;
        return _selectedEventDetail.AdditionalProperties.TryGetValue("categories", out var val) && val != null;
    }

    private IEnumerable<string> GetDetailCategories()
    {
        if (_selectedEventDetail?.AdditionalProperties == null) yield break;
        if (!_selectedEventDetail.AdditionalProperties.TryGetValue("categories", out var val) || val is not System.Text.Json.JsonElement jsonArray) yield break;
        if (jsonArray.ValueKind != System.Text.Json.JsonValueKind.Array) yield break;
        foreach (var item in jsonArray.EnumerateArray())
        {
            var name = item.TryGetProperty("fullName", out var fn) ? fn.GetString()
                     : item.TryGetProperty("name", out var n) ? n.GetString()
                     : null;
            if (!string.IsNullOrEmpty(name)) yield return name;
        }
    }

    // ── Event prev/next navigation ──

    private bool CanNavigatePrevEvent()
    {
        if (_selectedEvent?.Id == null) return false;
        var idx = _loadedEvents.FindIndex(e => e.Id == _selectedEvent.Id);
        return idx > 0;
    }

    private bool CanNavigateNextEvent()
    {
        if (_selectedEvent?.Id == null) return false;
        var idx = _loadedEvents.FindIndex(e => e.Id == _selectedEvent.Id);
        return idx >= 0 && idx < _loadedEvents.Count - 1;
    }

    private async Task NavigatePrevEvent()
    {
        if (_selectedEvent?.Id == null) return;
        var idx = _loadedEvents.FindIndex(e => e.Id == _selectedEvent.Id);
        if (idx > 0)
        {
            _showInlineRegistration = false;
            _showTagCatPopup = false;
            await SelectEvent(_loadedEvents[idx - 1]);
        }
    }

    private async Task NavigateNextEvent()
    {
        if (_selectedEvent?.Id == null) return;
        var idx = _loadedEvents.FindIndex(e => e.Id == _selectedEvent.Id);
        if (idx >= 0 && idx < _loadedEvents.Count - 1)
        {
            _showInlineRegistration = false;
            _showTagCatPopup = false;
            await SelectEvent(_loadedEvents[idx + 1]);
        }
    }

    // ── Inline registration ──

    private async Task OpenInlineRegistration()
    {
        if (_selectedEvent?.Id == null || _selectedEventDetail == null) return;

        _showInlineRegistration = true;
        _regIsLoading = true;
        _regIsComplete = false;
        _regIsAlreadyRegistered = false;
        _regShareEmail = false;
        _regShowConsentOption = false;
        _regSelectedSessionIds.Clear();

        try
        {
            if (IsUserRegistered(_selectedEvent.Id.Value))
            {
                _regIsAlreadyRegistered = true;
                _regIsLoading = false;
                return;
            }

            _regAvailableSessions = _selectedEventSessions;

            // Pre-select all sessions
            if (_regAvailableSessions != null)
            {
                foreach (var s in _regAvailableSessions.Where(s => s.Id.HasValue))
                    _regSelectedSessionIds.Add(s.Id!.Value);
            }

            // Get current user
            _regCurrentUser = await UserService.GetCurrentUserAsync();

            // Check consent
            _regOrganizerName = _selectedEventDetail.ActorDisplayName ?? "the organizer";
            if (_selectedEventDetail.ActorId.HasValue)
            {
                try
                {
                    var hasConsent = await ConsentService.CheckConsentForOrganizerAsync(_selectedEventDetail.ActorId.Value);
                    _regShowConsentOption = !hasConsent;
                }
                catch
                {
                    _regShowConsentOption = false;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error opening inline registration");
            Snackbar.Add("Could not load registration form.", Severity.Error);
            _showInlineRegistration = false;
        }
        finally
        {
            _regIsLoading = false;
        }
    }

    private void CloseInlineRegistration()
    {
        _showInlineRegistration = false;
        _regIsComplete = false;
        _regIsAlreadyRegistered = false;
    }

    private void ToggleRegSession(Guid sessionId)
    {
        if (!_regSelectedSessionIds.Remove(sessionId))
            _regSelectedSessionIds.Add(sessionId);
    }

    private void ToggleRegAllSessions()
    {
        if (_regAvailableSessions == null) return;
        var allIds = _regAvailableSessions.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();
        if (_regAllSessionsSelected)
            _regSelectedSessionIds.Clear();
        else
            _regSelectedSessionIds = new HashSet<Guid>(allIds);
    }

    private async Task HandleInlineRegistrationSubmit()
    {
        if (_regCurrentUser == null || !_regSelectedSessionIds.Any()) return;

        _regIsSubmitting = true;

        try
        {
            var consentText = _regShareEmail
                ? $"Share my email address with {_regOrganizerName} so they can contact me about future events and related updates."
                : null;

            bool allSucceeded = true;
            foreach (var sessionId in _regSelectedSessionIds)
            {
                var dto = new CreateEventRegistrationDto
                {
                    EventSessionId = sessionId,
                    UserId = _regCurrentUser.Id,
                };
                if (_regShareEmail && consentText != null)
                {
                    dto.AdditionalProperties["shareEmailWithOrganizer"] = true;
                    dto.AdditionalProperties["consentTextAcknowledged"] = consentText;
                    dto.AdditionalProperties["consentUiVersion"] = "v1";
                }

                var response = await EventService.RegisterForEventSessionAsync(dto);
                if (response?.Success != true)
                {
                    allSucceeded = false;
                    Snackbar.Add(response?.Message ?? "Registration failed for a session.", Severity.Warning);
                }
            }

            if (allSucceeded)
            {
                _regIsComplete = true;
                Snackbar.Add("Successfully registered!", Severity.Success);
                await LoadUserRegistrationsAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during inline registration");
            Snackbar.Add($"Registration error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _regIsSubmitting = false;
        }
    }

    // ── Tag/Category management ──

    private IEnumerable<TagCategoryManagerPopup.TagCategoryItem> GetDetailTagItems()
    {
        if (_selectedEventDetail?.AdditionalProperties == null) yield break;
        if (!_selectedEventDetail.AdditionalProperties.TryGetValue("tags", out var val) || val is not System.Text.Json.JsonElement jsonArray) yield break;
        if (jsonArray.ValueKind != System.Text.Json.JsonValueKind.Array) yield break;
        foreach (var item in jsonArray.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var g) ? g : (Guid?)null;
            var name = item.TryGetProperty("fullName", out var fn) ? fn.GetString()
                     : item.TryGetProperty("name", out var n) ? n.GetString()
                     : null;
            if (id.HasValue && !string.IsNullOrEmpty(name))
                yield return new TagCategoryManagerPopup.TagCategoryItem(id.Value, name);
        }
    }

    private IEnumerable<TagCategoryManagerPopup.TagCategoryItem> GetDetailCategoryItems()
    {
        if (_selectedEventDetail?.AdditionalProperties == null) yield break;
        if (!_selectedEventDetail.AdditionalProperties.TryGetValue("categories", out var val) || val is not System.Text.Json.JsonElement jsonArray) yield break;
        if (jsonArray.ValueKind != System.Text.Json.JsonValueKind.Array) yield break;
        foreach (var item in jsonArray.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var g) ? g : (Guid?)null;
            var name = item.TryGetProperty("fullName", out var fn) ? fn.GetString()
                     : item.TryGetProperty("name", out var n) ? n.GetString()
                     : null;
            if (id.HasValue && !string.IsNullOrEmpty(name))
                yield return new TagCategoryManagerPopup.TagCategoryItem(id.Value, name);
        }
    }

    private void OpenTagManagement()
    {
        _tagCatMode = TagCategoryMode.Tags;
        _tagCatInitialIds = GetDetailTagItems().Select(x => x.Id).ToList().AsReadOnly();
        _showTagCatPopup = true;
    }

    private void OpenCategoryManagement()
    {
        _tagCatMode = TagCategoryMode.Categories;
        _tagCatInitialIds = GetDetailCategoryItems().Select(x => x.Id).ToList().AsReadOnly();
        _showTagCatPopup = true;
    }

    private async Task HandleTagCatSaved(IReadOnlyCollection<Guid> newIds)
    {
        var label = _tagCatMode == TagCategoryMode.Tags ? "Tag" : "Category";
        Snackbar.Add($"{label} changes saved.", Severity.Success);

        if (_selectedEvent?.Id != null)
        {
            try
            {
                var detail = await EventService.GetEventByIdAsync(_selectedEvent.Id.Value);
                if (detail != null)
                {
                    _selectedEventDetail = detail;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error refreshing event after {Label} changes", label);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_imagePreloaderModule is not null)
        {
            await _imagePreloaderModule.DisposeAsync();
        }
    }

    public sealed class EventListState
    {
        public List<EventListDto> InitialItems { get; init; } = new();
        public int TotalCount { get; init; }
        public int InitialStartIndex { get; init; }
        public bool IsIslamicModuleEnabled { get; init; }
        public bool IsTechModuleEnabled { get; init; }
        public bool EventCardClickOpensDetailPage { get; init; }
        public List<EventTypeListDto> EventTypes { get; init; } = new();
        public List<AudienceGenderListDto> AudienceGenders { get; init; } = new();
        public List<AudienceAgeListDto> AudienceAges { get; init; } = new();
        public List<EventStatusListDto> EventStatuses { get; init; } = new();
        public List<EventFormatListDto> EventFormats { get; init; } = new();
        public List<CategoryListDto> Categories { get; init; } = new();
        public List<TagListDto> Tags { get; init; } = new();
        public List<MadhabListDto> Madhabs { get; init; } = new();
        public List<LocationListDto> Locations { get; init; } = new();
        public List<RegistrationModeListDto> RegistrationModes { get; init; } = new();
        public List<LanguageListDto> Languages { get; init; } = new();
        public List<TagTypeWithTagsDto> TagGroups { get; init; } = new();
        public List<CategoryTypeWithCategoriesDto> CategoryGroups { get; init; } = new();
    }
}
