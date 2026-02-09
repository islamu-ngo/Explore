using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Event;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Event;

public partial class EventList
{
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected ICategoryService CategoryService { get; set; } = null!;
    [Inject] protected ITagService TagService { get; set; } = null!;
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected ILocationService LocationService { get; set; } = null!;
    [Inject] protected IEventRegistrationService RegistrationService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ILogger<EventList> Logger { get; set; } = null!;

    private string? _errorMessage;
    private string? _successMessage;
    private string searchText = "";
    private string selectedDate = "";
    private Guid? selectedCategoryId;
    private Guid? selectedTagId;
    private int? selectedFormatId;
    private int? selectedMadhabId;
    private Guid? selectedLocationId;
    private int? selectedRegistrationModeId;
    private int? selectedLanguageId;
    private bool isLoading = true;
    private bool isLoadingCategory = false;
    private bool isLoadingTag = false;
    private bool _dataLoaded = false;

    // Load More variables
    private int displayedCount = 12;
    private const int loadBatchSize = 12;

    // Filter cache to avoid re-evaluating AllFilteredEvents multiple times per render
    private List<EventListDto> _cachedFilteredEvents = new();
    private bool _filtersDirty = true;

    // API Data
    private ICollection<EventListDto> allEvents = new List<EventListDto>();
    private ICollection<EventTypeListDto> eventTypes = new List<EventTypeListDto>();
    private ICollection<EventFormatListDto> eventFormats = new List<EventFormatListDto>();
    private ICollection<CategoryListDto> categories = new List<CategoryListDto>();
    private ICollection<TagListDto> tags = new List<TagListDto>();
    private ICollection<MadhabListDto> madhabs = new List<MadhabListDto>();
    private ICollection<LocationListDto> locations = new List<LocationListDto>();
    private ICollection<RegistrationModeListDto> registrationModes = new List<RegistrationModeListDto>();
    private ICollection<LanguageListDto> languages = new List<LanguageListDto>();
    private ICollection<EventSessionListDto> allSessions = new List<EventSessionListDto>();
    private ICollection<EventSessionLanguageListDto> sessionLanguages = new List<EventSessionLanguageListDto>();
    private Dictionary<int, string> eventTypeMap = new();
    private Dictionary<int, string> eventFormatMap = new();

    private ICollection<EventListDto>? eventsByCategory;
    private ICollection<EventListDto>? eventsByTag;

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
            searchText = SearchQuery;
            InvalidateFilterCache();
        }

        await LoadDataAsync();
        await LoadUserRegistrationsAsync();
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
            var eventsTask = EventService.GetAllEventsAsync();
            var eventTypesTask = EventService.GetEventTypesAsync();
            var eventFormatsTask = EventService.GetEventFormatsAsync();
            var categoriesTask = CategoryService.GetAllCategoriesAsync();
            var tagsTask = TagService.GetAllTagsAsync();
            var madhabsTask = AdminService.GetMadhabsAsync();
            var locationsTask = LocationService.GetAllLocationsAsync();
            var registrationModesTask = AdminService.GetRegistrationModesAsync();
            var languagesTask = AdminService.GetLanguagesAsync();
            var sessionsTask = EventService.GetAllSessionsAsync();
            var sessionLanguagesTask = EventService.GetAllSessionLanguagesAsync();

            await Task.WhenAll(eventsTask, eventTypesTask, eventFormatsTask, categoriesTask, tagsTask, madhabsTask, locationsTask, registrationModesTask, languagesTask, sessionsTask, sessionLanguagesTask);

            allEvents = await eventsTask ?? new List<EventListDto>();
            eventTypes = await eventTypesTask ?? new List<EventTypeListDto>();
            eventFormats = await eventFormatsTask ?? new List<EventFormatListDto>();
            categories = await categoriesTask ?? new List<CategoryListDto>();
            tags = await tagsTask ?? new List<TagListDto>();
            madhabs = await madhabsTask ?? new List<MadhabListDto>();
            locations = await locationsTask ?? new List<LocationListDto>();
            registrationModes = await registrationModesTask ?? new List<RegistrationModeListDto>();
            languages = await languagesTask ?? new List<LanguageListDto>();
            allSessions = await sessionsTask ?? new List<EventSessionListDto>();
            sessionLanguages = (await sessionLanguagesTask)?.Cast<EventSessionLanguageListDto>().ToList() ?? new List<EventSessionLanguageListDto>();

            BuildLookupMaps();
            InvalidateFilterCache();
            _dataLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadDataAsync error");
        }
        finally
        {
            isLoading = false;
        }
    }

    private List<EventListDto> AllFilteredEvents
    {
        get
        {
            if (_filtersDirty)
            {
                _cachedFilteredEvents = ComputeFilteredEvents();
                _filtersDirty = false;
            }
            return _cachedFilteredEvents;
        }
    }

    private void InvalidateFilterCache()
    {
        _filtersDirty = true;
        displayedCount = loadBatchSize; // Reset displayed count when filters change
    }

    private List<EventListDto> ComputeFilteredEvents()
    {
        var filteredEvents = allEvents ?? Enumerable.Empty<EventListDto>();

        if (!string.IsNullOrEmpty(searchText))
        {
            filteredEvents = filteredEvents.Where(e =>
                e.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (e.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (selectedCategoryId.HasValue)
        {
            if (eventsByCategory != null && eventsByCategory.Any())
            {
                var categoryEventIds = eventsByCategory.Select(e => e.Id).ToHashSet();
                filteredEvents = filteredEvents.Where(e => categoryEventIds.Contains(e.Id));
            }
            else if (!isLoadingCategory)
            {
                filteredEvents = Enumerable.Empty<EventListDto>();
            }
        }

        if (selectedTagId.HasValue)
        {
            if (eventsByTag != null && eventsByTag.Any())
            {
                var tagEventIds = eventsByTag.Select(e => e.Id).ToHashSet();
                filteredEvents = filteredEvents.Where(e => tagEventIds.Contains(e.Id));
            }
            else if (!isLoadingTag)
            {
                filteredEvents = Enumerable.Empty<EventListDto>();
            }
        }

        if (selectedFormatId.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.EventFormatId == selectedFormatId.Value);
        }

        if (selectedMadhabId.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.MadhabId == selectedMadhabId.Value);
        }

        if (selectedLocationId.HasValue && allSessions.Any())
        {
            var locationEventIds = allSessions
                .Where(s => s.LocationId == selectedLocationId.Value)
                .Select(s => s.EventId)
                .ToHashSet();
            filteredEvents = filteredEvents.Where(e => locationEventIds.Contains(e.Id));
        }

        if (selectedRegistrationModeId.HasValue && allSessions.Any())
        {
            var registrationEventIds = allSessions
                .Where(s => s.RegistrationModeId == selectedRegistrationModeId.Value)
                .Select(s => s.EventId)
                .ToHashSet();
            filteredEvents = filteredEvents.Where(e => registrationEventIds.Contains(e.Id));
        }

        if (selectedLanguageId.HasValue && allSessions.Any() && sessionLanguages.Any())
        {
            var languageSessionIds = sessionLanguages
                .Where(l => l.LanguageId == selectedLanguageId.Value)
                .Select(l => l.EventSessionId)
                .ToHashSet();
            var languageEventIds = allSessions
                .Where(s => languageSessionIds.Contains(s.Id))
                .Select(s => s.EventId)
                .ToHashSet();
            filteredEvents = filteredEvents.Where(e => languageEventIds.Contains(e.Id));
        }

        if (!string.IsNullOrEmpty(selectedDate))
        {
            var today = DateTimeOffset.Now.Date;
            filteredEvents = selectedDate switch
            {
                "today" => filteredEvents.Where(e => e.FirstSessionDate.HasValue && e.FirstSessionDate.Value.Date == today),
                "tomorrow" => filteredEvents.Where(e => e.FirstSessionDate.HasValue && e.FirstSessionDate.Value.Date == today.AddDays(1)),
                "thisweek" => filteredEvents.Where(e => e.FirstSessionDate.HasValue && e.FirstSessionDate.Value.Date >= today && e.FirstSessionDate.Value.Date <= today.AddDays(7)),
                "thismonth" => filteredEvents.Where(e => e.FirstSessionDate.HasValue && e.FirstSessionDate.Value.Date >= today && e.FirstSessionDate.Value.Date <= today.AddDays(30)),
                _ => filteredEvents
            };
        }

        return filteredEvents.ToList();
    }

    private List<EventListDto> FilteredEvents => AllFilteredEvents.Take(displayedCount).ToList();

    private bool HasMoreEvents => displayedCount < AllFilteredEvents.Count;

    private void LoadMore()
    {
        displayedCount += loadBatchSize;
        StateHasChanged();
    }

    private void OnDateChanged(string value)
    {
        selectedDate = value;
        InvalidateFilterCache();
    }

    private async Task OnCategoryChanged(Guid? categoryId)
    {
        selectedCategoryId = categoryId;

        if (categoryId.HasValue)
        {
            isLoadingCategory = true;
            try
            {
                var rawEvents = await CategoryService.GetEventsByCategoryAsync(categoryId.Value);
                eventsByCategory = new List<EventListDto>(); // Neutralized
            }
            finally
            {
                isLoadingCategory = false;
            }
        }
        else
        {
            eventsByCategory = null;
        }
        InvalidateFilterCache();
        StateHasChanged();
    }

    private async Task OnTagChanged(Guid? tagId)
    {
        selectedTagId = tagId;

        if (tagId.HasValue)
        {
            isLoadingTag = true;
            try
            {
                var rawEvents = await TagService.GetEventsByTagAsync(tagId.Value);
                eventsByTag = new List<EventListDto>(); // Neutralized
            }
            finally
            {
                isLoadingTag = false;
            }
        }
        else
        {
            eventsByTag = null;
        }
        InvalidateFilterCache();
        StateHasChanged();
    }

    private void OnFormatChanged(int? formatId)
    {
        selectedFormatId = formatId;
        InvalidateFilterCache();
    }

    private void OnMadhabChanged(int? madhabId)
    {
        selectedMadhabId = madhabId;
        InvalidateFilterCache();
    }

    private void OnLocationChanged(Guid? locationId)
    {
        selectedLocationId = locationId;
        InvalidateFilterCache();
    }

    private void OnRegistrationModeChanged(int? modeId)
    {
        selectedRegistrationModeId = modeId;
        InvalidateFilterCache();
    }

    private void OnLanguageChanged(int? languageId)
    {
        selectedLanguageId = languageId;
        InvalidateFilterCache();
    }

    // ... (helper methods like GetSelectedCategoryName can remain or be used for display)

    private async Task OpenDeleteDialog(EventListDto evt)
    {
        var parameters = new DialogParameters { ["EventId"] = evt.Id, ["EventTitle"] = evt.Title };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<DeleteEventDialog>("Delete Event", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled)
        {
            await LoadDataAsync();
            StateHasChanged();
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
            StateHasChanged();
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
        StateHasChanged();

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
            StateHasChanged();
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
}
