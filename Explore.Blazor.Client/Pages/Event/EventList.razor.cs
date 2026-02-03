using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Components.Event;
using Explore.Blazor.Client.Helpers;
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
                        foreach (var reg in registrations)
                        {
                            if (reg.EventSessionId.HasValue)
                            {
                                var session = await EventService.GetSessionByIdAsync(reg.EventSessionId.Value);
                                if (session != null && session.EventId.HasValue)
                                {
                                    eventIds.Add(session.EventId.Value);
                                }
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
            var categoriesTask = CategoryService.GetCategoriesAsync();
            var tagsTask = TagService.GetTagsAsync();
            var madhabsTask = AdminService.GetMadhabsAsync();
            var locationsTask = LocationService.GetLocations();
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
    }
    private List<EventListDto> FilteredEvents => AllFilteredEvents.Skip((currentPage - 1) * itemsPerPage).Take(itemsPerPage).ToList();

    private int TotalPages => AllFilteredEvents.Any() ? (int)Math.Ceiling((double)AllFilteredEvents.Count / itemsPerPage) : 1;

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
            try
            {
                // Note: GetEventsByCategoryAsync is neutralized and returns ICollection<object>
                // When API is updated, this can be properly typed
                var rawEvents = await CategoryService.GetEventsByCategoryAsync(categoryId.Value);
                eventsByCategory = new List<EventListDto>(); // Neutralized - returns empty
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
            try
            {
                // Note: GetEventsByTagAsync is neutralized and returns ICollection<object>
                // When API is updated, this can be properly typed
                var rawEvents = await TagService.GetEventsByTagAsync(tagId.Value);
                eventsByTag = new List<EventListDto>(); // Neutralized - returns empty
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
        return categories.FirstOrDefault(c => c.Id == selectedCategoryId.Value)?.FullName ?? "Category";
    }

    private string GetSelectedTagName()
    {
        if (!selectedTagId.HasValue) return "All Tags";
        return tags.FirstOrDefault(t => t.Id == selectedTagId.Value)?.FullName ?? "Tag";
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
        var location = locations.FirstOrDefault(l => l.Id == selectedLocationId.Value);
        if (location == null) return "Location";
        return !string.IsNullOrEmpty(location.City) ? $"{location.FullName} - {location.City}" : location.FullName;
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
        var parameters = new DialogParameters { ["EventId"] = evt.Id, ["EventTitle"] = evt.Title };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<DeleteEventDialog>("Delete Event", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled)
        {
            // This is tricky because we don't have a direct reference to the list.
            // We have to refetch or remove it from the source.
            // For now, just reload the data.
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
            Snackbar.Add("No sessions available for this event yet.", Severity.Warning);
            return;
        }
        var primarySession = sessions.First();
        var parameters = new DialogParameters { ["EventSessionId"] = primarySession.Id, ["Title"] = $"Register for {evt.Title}" };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<Explore.Blazor.Client.Components.EventRegistration>("Register", parameters, options);
        var result = await dialog.Result;
        if (result != null && !result.Canceled)
        {
            Snackbar.Add("Successfully registered for event!", Severity.Success);
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
        if (eventItem.EventFormatId == 1) return "Online";
        if (!string.IsNullOrEmpty(eventItem.EventFormatFullName)) return eventItem.EventFormatFullName;
        if (eventItem.EventFormatId.HasValue && eventFormatMap.TryGetValue(eventItem.EventFormatId.Value, out var formatName))
            return formatName;
        return "Location TBD";
    }

    private string GetEventImage(EventListDto eventItem)
    {
        if (!string.IsNullOrEmpty(eventItem.FeaturedImageUri)) return eventItem.FeaturedImageUri;
        var encodedTitle = Uri.EscapeDataString(eventItem.Title.Length > 30 ? eventItem.Title.Substring(0, 30) + "..." : eventItem.Title);
        var color = GetEventColorForEvent(eventItem);
        return $"https://placehold.co/600x400/{color}/ffffff?text={encodedTitle}";
    }

    private string GetEventColorForEvent(EventListDto eventItem)
    {
        var typeColors = new Dictionary<int, string>
        {
            { 1, "2196F3" }, { 2, "FF9800" }, { 3, "4CAF50" }, { 4, "E91E63" }, { 5, "9C27B0" },
        };
        if (eventItem.EventTypeId.HasValue && typeColors.TryGetValue(eventItem.EventTypeId.Value, out var color)) return color;
        var colors = new[] { "2196F3", "FF9800", "4CAF50", "E91E63", "9C27B0", "607D8B" };
        return colors[Math.Abs(eventItem.Title.GetHashCode()) % colors.Length];
    }

    private string GetTruncatedDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return "No description available.";
        var sentences = System.Text.RegularExpressions.Regex.Split(description, @"(?<=[.!?])\s+");
        var result = string.Join(" ", sentences.Take(2));
        if (sentences.Length > 2) result += "...";
        if (result.Length > 150) result = result.Substring(0, 147) + "...";
        return result;
    }

    private string GetActorInitials(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return "?";
        var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2) return $"{words[0][0]}{words[1][0]}".ToUpperInvariant();
        return displayName.Length >= 2 ? displayName.Substring(0, 2).ToUpperInvariant() : displayName.ToUpperInvariant();
    }
}
