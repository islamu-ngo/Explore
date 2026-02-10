using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Event;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
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
    private bool _dataLoaded = false;

    private Virtualize<EventListDto>? _virtualize;
    private int _totalCount;

    // API Data
    private ICollection<EventTypeListDto> eventTypes = new List<EventTypeListDto>();
    private ICollection<EventFormatListDto> eventFormats = new List<EventFormatListDto>();
    private ICollection<CategoryListDto> categories = new List<CategoryListDto>();
    private ICollection<TagListDto> tags = new List<TagListDto>();
    private ICollection<MadhabListDto> madhabs = new List<MadhabListDto>();
    private ICollection<LocationListDto> locations = new List<LocationListDto>();
    private ICollection<RegistrationModeListDto> registrationModes = new List<RegistrationModeListDto>();
    private ICollection<LanguageListDto> languages = new List<LanguageListDto>();
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
            var eventTypesTask = EventService.GetEventTypesAsync();
            var eventFormatsTask = EventService.GetEventFormatsAsync();
            var categoriesTask = CategoryService.GetAllCategoriesAsync();
            var tagsTask = TagService.GetAllTagsAsync();
            var madhabsTask = AdminService.GetMadhabsAsync();
            var locationsTask = LocationService.GetAllLocationsAsync();
            var registrationModesTask = AdminService.GetRegistrationModesAsync();
            var languagesTask = AdminService.GetLanguagesAsync();

            await Task.WhenAll(eventTypesTask, eventFormatsTask, categoriesTask, tagsTask, madhabsTask, locationsTask, registrationModesTask, languagesTask);

            eventTypes = await eventTypesTask ?? new List<EventTypeListDto>();
            eventFormats = await eventFormatsTask ?? new List<EventFormatListDto>();
            categories = await categoriesTask ?? new List<CategoryListDto>();
            tags = await tagsTask ?? new List<TagListDto>();
            madhabs = await madhabsTask ?? new List<MadhabListDto>();
            locations = await locationsTask ?? new List<LocationListDto>();
            registrationModes = await registrationModesTask ?? new List<RegistrationModeListDto>();
            languages = await languagesTask ?? new List<LanguageListDto>();

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

    private async ValueTask<ItemsProviderResult<EventListDto>> LoadEventsAsync(ItemsProviderRequest request)
    {
        var pageSize = Math.Max(request.Count, 20);
        var pageNumber = (request.StartIndex / pageSize) + 1;

        DateTimeOffset? dateFrom = null;
        DateTimeOffset? dateTo = null;
        if (!string.IsNullOrEmpty(selectedDate))
        {
            var today = DateTimeOffset.Now.Date;
            (dateFrom, dateTo) = selectedDate switch
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
            searchTerm: string.IsNullOrEmpty(searchText) ? null : searchText,
            categoryId: selectedCategoryId,
            tagId: selectedTagId,
            formatId: selectedFormatId,
            madhabId: selectedMadhabId,
            locationId: selectedLocationId,
            registrationModeId: selectedRegistrationModeId,
            languageId: selectedLanguageId,
            dateFrom: dateFrom,
            dateTo: dateTo,
            sortBy: "date",
            sortDescending: true,
            cancellationToken: request.CancellationToken);

        _totalCount = result.TotalCount;
        isLoading = false;
        return new ItemsProviderResult<EventListDto>(result.Items, result.TotalCount);
    }

    private async Task OnDateChanged(string value)
    {
        selectedDate = value;
        await _virtualize?.RefreshDataAsync()!;
    }

    private async Task OnCategoryChanged(Guid? categoryId)
    {
        selectedCategoryId = categoryId;
        await _virtualize?.RefreshDataAsync()!;
    }

    private async Task OnTagChanged(Guid? tagId)
    {
        selectedTagId = tagId;
        await _virtualize?.RefreshDataAsync()!;
    }

    private async Task OnFormatChanged(int? formatId)
    {
        selectedFormatId = formatId;
        await _virtualize?.RefreshDataAsync()!;
    }

    private async Task OnMadhabChanged(int? madhabId)
    {
        selectedMadhabId = madhabId;
        await _virtualize?.RefreshDataAsync()!;
    }

    private async Task OnLocationChanged(Guid? locationId)
    {
        selectedLocationId = locationId;
        await _virtualize?.RefreshDataAsync()!;
    }

    private async Task OnRegistrationModeChanged(int? modeId)
    {
        selectedRegistrationModeId = modeId;
        await _virtualize?.RefreshDataAsync()!;
    }

    private async Task OnLanguageChanged(int? languageId)
    {
        selectedLanguageId = languageId;
        await _virtualize?.RefreshDataAsync()!;
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
}
