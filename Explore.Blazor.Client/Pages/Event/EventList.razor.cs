using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Components.Event;
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
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected ILogger<EventList> Logger { get; set; } = null!;

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

    // Pagination variables
    private int currentPage = 1;
    private int itemsPerPage = 6;

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

    [SupplyParameterFromQuery(Name = "q")]
    public string? SearchQuery { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogDebug("OnInitializedAsync starting");

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            searchText = SearchQuery;
        }

        // Always load data - don't use any caching that can cause issues
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
                if (user != null)
                {
                    var registrations = await EventService.GetRegistrationsByUserAsync(user.Id);
                    if (registrations != null)
                    {
                        var eventIds = new HashSet<Guid>();
                        foreach (var reg in registrations)
                        {
                            var session = await EventService.GetSessionByIdAsync(reg.EventSessionId);
                            if (session != null)
                            {
                                eventIds.Add(session.EventId);
                            }
                        }
                        _registeredEventIds = eventIds;
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
        eventTypeMap = eventTypes.Any()
            ? eventTypes.ToDictionary(et => et.Id, et => et.FullName)
            : new();
        eventFormatMap = eventFormats.Any()
            ? eventFormats.ToDictionary(pt => pt.Id, pt => pt.FullName)
            : new();
    }

    private async Task LoadDataAsync()
    {
        // Prevent multiple loads
        if (_dataLoaded) return;

        isLoading = true;

        try
        {
            Logger.LogDebug("Loading data");

            // Parallel loading
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

            allEvents = await eventsTask;
            eventTypes = await eventTypesTask;
            eventFormats = await eventFormatsTask;
            categories = await categoriesTask;
            tags = await tagsTask;
            madhabs = await madhabsTask;
            locations = await locationsTask;
            registrationModes = await registrationModesTask;
            languages = await languagesTask;
            allSessions = await sessionsTask;
            sessionLanguages = await sessionLanguagesTask;

            Logger.LogDebug("Loaded {EventCount} events, {TypeCount} types, {FormatCount} formats, {CategoryCount} categories, {TagCount} tags, {MadhabCount} madhabs, {LocationCount} locations, {ModeCount} registration modes, {LanguageCount} languages, {SessionCount} sessions, {SessionLanguageCount} session languages",
                allEvents.Count, eventTypes.Count, eventFormats.Count, categories.Count, tags.Count, madhabs.Count, locations.Count, registrationModes.Count, languages.Count, allSessions.Count, sessionLanguages.Count);

            BuildLookupMaps();
            _dataLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadDataAsync error");
            // Keep empty lists on error - don't throw
            allEvents = new List<EventListDto>();
            eventTypes = new List<EventTypeListDto>();
            eventFormats = new List<EventFormatListDto>();
            categories = new List<CategoryListDto>();
            tags = new List<TagListDto>();
            madhabs = new List<MadhabListDto>();
            locations = new List<LocationListDto>();
            registrationModes = new List<RegistrationModeListDto>();
            languages = new List<LanguageListDto>();
            allSessions = new List<EventSessionListDto>();
            sessionLanguages = new List<EventSessionLanguageListDto>();
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
            var filteredEvents = allEvents.AsEnumerable();

            // Search filter
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredEvents = filteredEvents.Where(e =>
                    e.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (e.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Category filter - if we have events by category loaded
            if (selectedCategoryId.HasValue)
            {
                if (eventsByCategory != null && eventsByCategory.Any())
                {
                    var categoryEventIds = eventsByCategory.Select(e => e.Id).ToHashSet();
                    filteredEvents = filteredEvents.Where(e => categoryEventIds.Contains(e.Id));
                }
                else if (!isLoadingCategory)
                {
                    // Category selected but no events found - show empty
                    filteredEvents = Enumerable.Empty<EventListDto>();
                }
            }

            // Tag filter - if we have events by tag loaded
            if (selectedTagId.HasValue)
            {
                if (eventsByTag != null && eventsByTag.Any())
                {
                    var tagEventIds = eventsByTag.Select(e => e.Id).ToHashSet();
                    filteredEvents = filteredEvents.Where(e => tagEventIds.Contains(e.Id));
                }
                else if (!isLoadingTag)
                {
                    // Tag selected but no events found - show empty
                    filteredEvents = Enumerable.Empty<EventListDto>();
                }
            }

            // Format filter
            if (selectedFormatId.HasValue)
            {
                filteredEvents = filteredEvents.Where(e => e.EventFormatId == selectedFormatId.Value);
            }

            // Madhab filter
            if (selectedMadhabId.HasValue)
            {
                filteredEvents = filteredEvents.Where(e => e.MadhabId == selectedMadhabId.Value);
            }

            // Location filter
            if (selectedLocationId.HasValue && allSessions != null && allSessions.Any())
            {
                var locationEventIds = allSessions
                    .Where(s => s.LocationId == selectedLocationId.Value)
                    .Select(s => s.EventId)
                    .ToHashSet();
                filteredEvents = filteredEvents.Where(e => locationEventIds.Contains(e.Id));
            }

            // Registration mode filter
            if (selectedRegistrationModeId.HasValue && allSessions != null && allSessions.Any())
            {
                var registrationEventIds = allSessions
                    .Where(s => s.RegistrationModeId == selectedRegistrationModeId.Value)
                    .Select(s => s.EventId)
                    .ToHashSet();
                filteredEvents = filteredEvents.Where(e => registrationEventIds.Contains(e.Id));
            }

            // Language filter
            if (selectedLanguageId.HasValue && allSessions != null && allSessions.Any() && sessionLanguages != null && sessionLanguages.Any())
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

            // Date filter - use DateTimeOffset for proper timezone handling
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
    }

    private List<EventListDto> FilteredEvents
    {
        get
        {
            var allEvents = AllFilteredEvents;
            return allEvents
                .Skip((currentPage - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();
        }
    }

    private int TotalPages
    {
        get
        {
            var count = AllFilteredEvents.Count;
            return count > 0 ? (int)Math.Ceiling((double)count / itemsPerPage) : 1;
        }
    }

    private void OnDateChanged(string value)
    {
        selectedDate = value;
        currentPage = 1;
    }

    private async Task OnCategoryChanged(Guid? categoryId)
    {
        selectedCategoryId = categoryId;
        currentPage = 1;

        if (categoryId.HasValue)
        {
            isLoadingCategory = true;
            StateHasChanged();
            try
            {
                eventsByCategory = await CategoryService.GetEventsByCategoryAsync(categoryId.Value);
                Logger.LogDebug("Category filter: loaded {Count} events for category {CategoryId}", eventsByCategory?.Count ?? 0, categoryId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading events for category {CategoryId}", categoryId);
                eventsByCategory = new List<EventListDto>();
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
        StateHasChanged();
    }

    private async Task OnTagChanged(Guid? tagId)
    {
        selectedTagId = tagId;
        currentPage = 1;

        if (tagId.HasValue)
        {
            isLoadingTag = true;
            StateHasChanged();
            try
            {
                eventsByTag = await TagService.GetEventsByTagAsync(tagId.Value);
                Logger.LogDebug("Tag filter: loaded {Count} events for tag {TagId}", eventsByTag?.Count ?? 0, tagId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading events for tag {TagId}", tagId);
                eventsByTag = new List<EventListDto>();
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
        StateHasChanged();
    }

    private void OnFormatChanged(int? formatId)
    {
        selectedFormatId = formatId;
        currentPage = 1;
    }

    private void OnMadhabChanged(int? madhabId)
    {
        selectedMadhabId = madhabId;
        currentPage = 1;
    }

    private void OnLocationChanged(Guid? locationId)
    {
        selectedLocationId = locationId;
        currentPage = 1;
    }

    private void OnRegistrationModeChanged(int? modeId)
    {
        selectedRegistrationModeId = modeId;
        currentPage = 1;
    }

    private void OnLanguageChanged(int? languageId)
    {
        selectedLanguageId = languageId;
        currentPage = 1;
    }

    private string GetSelectedCategoryName()
    {
        if (!selectedCategoryId.HasValue) return "All Categories";
        return categories?.FirstOrDefault(c => c.Id == selectedCategoryId.Value)?.FullName ?? "Category";
    }

    private string GetSelectedTagName()
    {
        if (!selectedTagId.HasValue) return "All Tags";
        return tags?.FirstOrDefault(t => t.Id == selectedTagId.Value)?.FullName ?? "Tag";
    }

    private string GetSelectedFormatName()
    {
        if (!selectedFormatId.HasValue) return "All Formats";
        return eventFormats?.FirstOrDefault(f => f.Id == selectedFormatId.Value)?.FullName ?? "Format";
    }

    private string GetSelectedMadhabName()
    {
        if (!selectedMadhabId.HasValue) return "All Madhabs";
        return madhabs?.FirstOrDefault(m => m.Id == selectedMadhabId.Value)?.FullName ?? "Madhab";
    }

    private string GetSelectedLocationName()
    {
        if (!selectedLocationId.HasValue) return "All Locations";
        var location = locations?.FirstOrDefault(l => l.Id == selectedLocationId.Value);
        if (location == null) return "Location";
        return !string.IsNullOrEmpty(location.City)
            ? $"{location.FullName} - {location.City}"
            : location.FullName;
    }

    private string GetSelectedRegistrationModeName()
    {
        if (!selectedRegistrationModeId.HasValue) return "All Modes";
        return registrationModes?.FirstOrDefault(m => m.Id == selectedRegistrationModeId.Value)?.FullName ?? "Mode";
    }

    private string GetSelectedLanguageName()
    {
        if (!selectedLanguageId.HasValue) return "All Languages";
        return languages?.FirstOrDefault(l => l.Id == selectedLanguageId.Value)?.FullName ?? "Language";
    }

    private void OnPageChanged(int page)
    {
        currentPage = page;
    }

    private async Task OpenDeleteDialog(EventListDto evt)
    {
        var parameters = new DialogParameters
        {
            { "EventId", evt.Id },
            { "EventTitle", evt.Title }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<DeleteEventDialog>("Delete Event", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            // Refresh the list
            allEvents = allEvents.Where(e => e.Id != evt.Id).ToList();
            StateHasChanged();
        }
    }

    private async Task OpenQuickRegisterDialog(EventListDto evt)
    {
        // First get the sessions for this event
        var sessions = await EventService.GetSessionsByEventAsync(evt.Id);

        if (sessions == null || !sessions.Any())
        {
            Snackbar.Add("No sessions available for this event yet.", Severity.Warning);
            return;
        }

        var primarySession = sessions.First();

        var parameters = new DialogParameters
        {
            { "EventSessionId", primarySession.Id },
            { "Title", $"Register for {evt.Title}" }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<Explore.Blazor.Client.Components.EventRegistration>(
            "Register",
            parameters,
            options
        );

        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Snackbar.Add("Successfully registered for event!", Severity.Success);
        }
    }

    private string GetEventTypeName(EventListDto eventItem)
    {
        // Use the FullName from the DTO directly if available
        if (!string.IsNullOrEmpty(eventItem.EventTypeFullName))
            return eventItem.EventTypeFullName;

        // Fallback to lookup
        return eventTypeMap.TryGetValue(eventItem.EventTypeId, out var eventTypeName)
            ? eventTypeName
            : "Event";
    }

    private string GetLocationText(EventListDto eventItem)
    {
        // EventListDto doesn't have location directly - use format
        if (eventItem.EventFormatId == 1 // Assuming 1 = Online
            ) return "Online";

        if (!string.IsNullOrEmpty(eventItem.EventFormatFullName))
            return eventItem.EventFormatFullName;

        return eventFormatMap.TryGetValue(eventItem.EventFormatId, out var formatName)
            ? formatName
            : "Location TBD";
    }

    private string GetEventImage(EventListDto eventItem)
    {
        // Use FeaturedImageUri if available
        if (!string.IsNullOrEmpty(eventItem.FeaturedImageUri))
            return eventItem.FeaturedImageUri;

        // Fallback to placeholder
        var encodedTitle = Uri.EscapeDataString(eventItem.Title.Length > 30
            ? eventItem.Title.Substring(0, 30) + "..."
            : eventItem.Title);
        var color = GetEventColorForEvent(eventItem);
        return $"https://placehold.co/600x400/{color}/ffffff?text={encodedTitle}";
    }

    private string GetEventColorForEvent(EventListDto eventItem)
    {
        // Quick color hash based on event type
        var typeColors = new Dictionary<int, string>
        {
            { 1, "2196F3" }, // Lecture - Blue
            { 2, "FF9800" }, // Workshop - Orange
            { 3, "4CAF50" }, // Social - Green
            { 4, "E91E63" }, // Charity - Pink
            { 5, "9C27B0" }, // Education - Purple
        };

        if (typeColors.TryGetValue(eventItem.EventTypeId, out var color))
            return color;

        // Fallback: hash of title
        var hash = eventItem.Title.GetHashCode();
        var colors = new[] { "2196F3", "FF9800", "4CAF50", "E91E63", "9C27B0", "607D8B" };
        return colors[Math.Abs(hash) % colors.Length];
    }

    private string GetTruncatedDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "No description available.";

        // Split op zinnen (. ! ?)
        var sentences = System.Text.RegularExpressions.Regex.Split(description, @"(?<=[.!?])\s+");

        // Neem maximaal 2 zinnen
        var result = string.Join(" ", sentences.Take(2));

        // Als er meer dan 2 zinnen zijn, voeg ... toe
        if (sentences.Length > 2)
            result += "...";

        // Extra veiligheid: limiteer tot 150 karakters
        if (result.Length > 150)
            result = result.Substring(0, 147) + "...";

        return result;
    }

    private string GetActorInitials(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "?";

        var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
            return $"{words[0][0]}{words[1][0]}".ToUpperInvariant();

        return displayName.Length >= 2
            ? displayName.Substring(0, 2).ToUpperInvariant()
            : displayName.ToUpperInvariant();
    }
}
