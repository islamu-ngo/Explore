// ABOUTME: Event list page logic for loading filters, events, and registrations.
// ABOUTME: Preserves initial prerender results to avoid hydration flicker on SEO pages.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components;
using Explore.Blazor.Client.Components.Event;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Event;

public partial class EventList : ComponentBase
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

    [PersistentState]
    public EventListState? PersistedState { get; set; }

    private EventFilterBar? _filterBar;
    private TriStateTagFilterDropdown? _tagFilterDropdown;
    private string? _errorMessage;
    private string? _successMessage;

    // Module Flags
    private bool _isIslamicModuleEnabled;
    private bool _isTechModuleEnabled;

    private bool isLoading = true;
    private bool _eventsLoaded = false;
    private bool _dataLoaded = false;
    private bool _usePersistedEvents = false;

    private Virtualize<EventListDto>? _virtualize;
    private int _totalCount;

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

    private Dictionary<int, string> eventTypeMap = new();
    private Dictionary<int, string> eventFormatMap = new();

    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private HashSet<Guid> _registeredEventIds = new();
    private Dictionary<Guid, Guid> _registrationIdByEventId = new();
    private bool _isCancellingRegistration = false;

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
        }

        await LoadDataAsync();
        await LoadUserRegistrationsAsync();
    }

    private bool TryRestoreState()
    {
        if (PersistedState == null)
        {
            return false;
        }

        _isIslamicModuleEnabled = PersistedState.IsIslamicModuleEnabled;
        _isTechModuleEnabled = PersistedState.IsTechModuleEnabled;

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

            await Task.WhenAll(eventTypesTask, audienceGendersTask, audienceAgesTask, eventStatusesTask, eventFormatsTask, categoriesTask, tagsTask, madhabsTask, locationsTask, registrationModesTask, languagesTask, tagGroupsTask);

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

    private async ValueTask<ItemsProviderResult<EventListDto>> LoadEventsAsync(ItemsProviderRequest request)
    {
        if (_usePersistedEvents && PersistedState != null && request.StartIndex == PersistedState.InitialStartIndex)
        {
            _usePersistedEvents = false;
            return new ItemsProviderResult<EventListDto>(PersistedState.InitialItems, PersistedState.TotalCount);
        }

        var pageSize = Math.Max(request.Count, 20);
        var pageNumber = (request.StartIndex / pageSize) + 1;

        // Get filter values from _filterBar or defaults
        var dateFilter = _filterBar?.SelectedDate ?? "";
        var searchTerm = _filterBar?.SearchTerm ?? SearchQuery;
        var categoryId = _filterBar?.SelectedCategoryId;
        // Tag filter state will be provided by TriStateTagFilterDropdown in Phase 4/5
        // For now, pass null for multi-tag params
        var formatId = _filterBar?.SelectedFormatId;
        var madhabId = _filterBar?.SelectedMadhabId;
        var locationId = _filterBar?.SelectedLocationId;
        var registrationModeId = _filterBar?.SelectedRegistrationModeId;
        var languageId = _filterBar?.SelectedLanguageId;

        var eventTypeId = _filterBar?.SelectedEventTypeId;
        var audienceGenderId = _filterBar?.SelectedAudienceGenderId;
        var audienceAgeId = _filterBar?.SelectedAudienceAgeId;
        var eventStatusId = _filterBar?.SelectedEventStatusId;
        var sortBy = _filterBar?.SelectedSortBy ?? "date";
        var sortDescending = _filterBar?.SortDescending ?? true;

        // Islamic
        var genderModeId = _filterBar?.SelectedGenderMode != null ? (int?)_filterBar.SelectedGenderMode : null;
        var includesQuran = _filterBar?.IncludesQuranRecitation;
        var prayerId = _filterBar?.SelectedReferencePrayer != null ? (int?)_filterBar.SelectedReferencePrayer : null;
        var islamicLangId = _filterBar?.SelectedIslamicPrimaryLanguageId;
        var hasIslamic = _filterBar?.HasIslamicAspect;

        // Tech
        var skillLevelId = _filterBar?.SelectedSkillLevel != null ? (int?)_filterBar.SelectedSkillLevel : null;
        var codingComp = _filterBar?.IsCodingCompetition;
        var hackathon = _filterBar?.IsHackathon;
        var laptop = _filterBar?.RequiresLaptop;
        var techStack = _filterBar?.TechStackTag;
        var hasTech = _filterBar?.HasTechAspect;

        DateTimeOffset? dateFrom = null;
        DateTimeOffset? dateTo = null;
        if (!string.IsNullOrEmpty(dateFilter))
        {
            var today = DateTimeOffset.Now.Date;
            (dateFrom, dateTo) = dateFilter switch
            {
                "today" => ((DateTimeOffset?)today, (DateTimeOffset?)today.AddDays(1).AddTicks(-1)),
                "tomorrow" => ((DateTimeOffset?)today.AddDays(1), (DateTimeOffset?)today.AddDays(2).AddTicks(-1)),
                "thisweek" => ((DateTimeOffset?)today, (DateTimeOffset?)today.AddDays(7)),
                "thismonth" => ((DateTimeOffset?)today, (DateTimeOffset?)today.AddDays(30)),
                _ => (null, null)
            };
        }

        var result = await EventService.GetEventsPagedAsync(
            pageNumber,
            pageSize,
            searchTerm: searchTerm,
            categoryId: categoryId,
            includedTagIds: _tagFilterDropdown?.GetCurrentFilter().IncludedTagIds,
            excludedTagIds: _tagFilterDropdown?.GetCurrentFilter().ExcludedTagIds,
            inclusionMode: _tagFilterDropdown?.GetCurrentFilter().InclusionMode,
            exclusionMode: _tagFilterDropdown?.GetCurrentFilter().ExclusionMode,
            formatId: formatId,
            madhabId: madhabId,
            locationId: locationId,
            registrationModeId: registrationModeId,
            languageId: languageId,
            dateFrom: dateFrom,
            dateTo: dateTo,
            sortBy: sortBy,
            sortDescending: sortDescending,
            eventTypeId: eventTypeId,
            audienceGenderId: audienceGenderId,
            audienceAgeId: audienceAgeId,
            eventStatusId: eventStatusId,
            genderModeId: genderModeId,
            includesQuranRecitation: includesQuran,
            referencePrayerId: prayerId,
            islamicPrimaryLanguageId: islamicLangId,
            hasIslamicAspect: hasIslamic,
            skillLevelId: skillLevelId,
            isCodingCompetition: codingComp,
            isHackathon: hackathon,
            requiresLaptop: laptop,
            techStackTag: techStack,
            hasTechAspect: hasTech,
            cancellationToken: request.CancellationToken);

        _totalCount = result.TotalCount;
        _eventsLoaded = true;
        // Do not set isLoading = false here, it's controlled by LoadDataAsync for the initial skeletons
        // But if we want to hide skeletons AFTER first load of events, we need a separate flag?
        // Actually, isLoading is used for Skeletons.
        // Let's set isLoading = false here to ensure skeletons disappear if they were still showing.
        if (isLoading) isLoading = false;
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
                Languages = languages.ToList()
            };
        }

        return new ItemsProviderResult<EventListDto>(result.Items, result.TotalCount);
    }

    private async Task RefreshList()
    {
        if (_virtualize != null)
        {
            await _virtualize.RefreshDataAsync();
        }
    }

    // ... (helper methods like GetSelectedCategoryName can remain or be used for display)

    private async Task OpenDeleteDialog(EventListDto evt)
    {
        var parameters = new DialogParameters { ["EventId"] = evt.Id, ["EventTitle"] = evt.Title };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<Explore.Blazor.Client.Components.Event.DeleteEventDialog>("Delete Event", parameters, options);
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
        var dialog = await DialogService.ShowAsync<Explore.Blazor.Client.Components.EventRegistration>("Register", parameters, options);
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

        var confirm = await DialogService.ShowMessageBox(
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

    public sealed class EventListState
    {
        public List<EventListDto> InitialItems { get; init; } = new();
        public int TotalCount { get; init; }
        public int InitialStartIndex { get; init; }
        public bool IsIslamicModuleEnabled { get; init; }
        public bool IsTechModuleEnabled { get; init; }
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
    }
}
