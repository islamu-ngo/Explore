using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Event;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Event;

/// <summary>
/// Displays events created by organizations the current user is a member of.
/// Provides filtering, searching, and management capabilities.
/// </summary>
public partial class MyEvents : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILogger<MyEvents> Logger { get; set; } = default!;

    // Data
    private ICollection<EventListDto> _events = new List<EventListDto>();
    private ICollection<OrganizationListDto> _myOrganizations = new List<OrganizationListDto>();

    // UI State
    private bool _isLoading = true;
    private string? _errorMessage;
    private string _searchString = string.Empty;
    private string _selectedCategory = string.Empty;
    private Guid _selectedOrganizationId = Guid.Empty;

    // Filter dropdowns
    private bool _isOrganizationOpen;
    private bool _isCategoryOpen;

    // Pagination
    private int _currentPage = 1;
    private const int ItemsPerPage = 6;

    /// <summary>
    /// Gets unique event types for filtering.
    /// </summary>
    private List<string> UniqueEventTypes => _events
        .Select(e => e.EventTypeFullName)
        .Where(t => !string.IsNullOrEmpty(t))
        .Distinct()
        .OrderBy(t => t)
        .ToList()!;

    /// <summary>
    /// Gets all filtered events (before pagination).
    /// </summary>
    private List<EventListDto> AllFilteredEvents => _events
        .Where(e => string.IsNullOrEmpty(_searchString) ||
                    e.Title.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                    (e.Description?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(e => string.IsNullOrEmpty(_selectedCategory) ||
                    (e.EventTypeFullName?.Contains(_selectedCategory, StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(e => _selectedOrganizationId == Guid.Empty ||
                    e.ActorId == _selectedOrganizationId)
        .ToList();

    /// <summary>
    /// Gets paginated filtered events.
    /// </summary>
    private List<EventListDto> FilteredEvents => AllFilteredEvents
        .Skip((_currentPage - 1) * ItemsPerPage)
        .Take(ItemsPerPage)
        .ToList();

    /// <summary>
    /// Gets total number of pages.
    /// </summary>
    private int TotalPages
    {
        get
        {
            var count = AllFilteredEvents.Count;
            return count > 0 ? (int)Math.Ceiling((double)count / ItemsPerPage) : 1;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("[MyEvents] OnInitializedAsync starting...");
        await LoadDataAsync();
    }

    /// <summary>
    /// Loads events and organizations in parallel.
    /// </summary>
    private async Task LoadDataAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            Logger.LogInformation("[MyEvents] Loading events and organizations...");

            var eventsTask = EventService.GetMyEventsAsync();
            var organizationsTask = OrganizationService.GetMyOrganizationsAsync();

            await Task.WhenAll(eventsTask, organizationsTask);

            _events = await eventsTask;
            _myOrganizations = await organizationsTask;

            Logger.LogInformation("[MyEvents] Loaded {EventCount} events and {OrgCount} organizations",
                _events.Count, _myOrganizations.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[MyEvents] Error loading data");
            _errorMessage = "Unable to load your events. Please try again.";
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Retry loading data after an error.
    /// </summary>
    private async Task RetryLoadAsync()
    {
        _errorMessage = null;
        await LoadDataAsync();
    }

    /// <summary>
    /// Checks if user can edit the given event based on their organization role.
    /// </summary>
    private bool CanEditEvent(EventListDto evt)
    {
        var org = _myOrganizations.FirstOrDefault(o => o.Id == evt.ActorId);
        if (org == null) return false;

        // Creator (1), CoOwner (2), Admin (3) can edit
        return org.CurrentUserRole is 1 or 2 or 3;
    }

    /// <summary>
    /// Navigate to event edit page.
    /// </summary>
    private void EditEvent(EventListDto evt)
    {
        if (CanEditEvent(evt))
        {
            Navigation.NavigateTo($"/eventedit/{evt.Id}");
        }
        else
        {
            Snackbar.Add("You do not have permission to edit this event.", Severity.Error);
        }
    }

    /// <summary>
    /// Navigate to event detail page.
    /// </summary>
    private void ViewEventDetails(EventListDto evt)
    {
        Navigation.NavigateTo($"/event/detail/{evt.Id}");
    }

    /// <summary>
    /// Open registration manager dialog for the event.
    /// </summary>
    private async Task ViewRegistrations(EventListDto evt)
    {
        var sessionParameters = new DialogParameters { ["EventId"] = evt.Id };
        var sessionDialog = await DialogService.ShowAsync<SelectSessionDialog>(
            $"Select a session for {evt.Title}", sessionParameters);
        var sessionResult = await sessionDialog.Result;

        if (sessionResult is { Canceled: false, Data: Guid selectedSessionId })
        {
            var parameters = new DialogParameters { ["EventSessionId"] = selectedSessionId };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
            var dialog = DialogService.Show<RegistrationManagerDialog>("Registrations for session", parameters, options);
            await dialog.Result;
        }
    }

    /// <summary>
    /// Delete an event after confirmation.
    /// </summary>
    private async Task DeleteEvent(EventListDto evt)
    {
        if (!CanEditEvent(evt))
        {
            Snackbar.Add("You do not have permission to delete this event.", Severity.Error);
            return;
        }

        try
        {
            var result = await DialogService.ShowMessageBox(
                "Delete Event",
                $"Are you sure you want to delete '{evt.Title}'? This action cannot be undone.",
                yesText: "Delete",
                cancelText: "Cancel",
                options: new DialogOptions { MaxWidth = MaxWidth.Small });

            if (result == true)
            {
                Logger.LogInformation("[MyEvents] Deleting event {EventId}", evt.Id);
                var success = await EventService.DeleteEventAsync(evt.Id);

                if (success)
                {
                    Snackbar.Add($"Event '{evt.Title}' has been deleted.", Severity.Success);
                    _events.Remove(evt);

                    // Reset to first page if current page is now empty
                    if (!FilteredEvents.Any() && _currentPage > 1)
                    {
                        _currentPage = 1;
                    }

                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add("Failed to delete event. You may not have permission.", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[MyEvents] Error deleting event {EventId}", evt.Id);
            Snackbar.Add($"Error deleting event: {ex.Message}", Severity.Error);
        }
    }

    // Filter handlers
    private void OnSearch(string value)
    {
        _searchString = value;
        _currentPage = 1;
    }

    private void OnCategoryChanged(string value)
    {
        _selectedCategory = value;
        _isCategoryOpen = false;
        _currentPage = 1;
    }

    private void OnOrganizationChanged(Guid organizationId)
    {
        _selectedOrganizationId = organizationId;
        _isOrganizationOpen = false;
        _currentPage = 1;
    }

    private void OnPageChanged(int page)
    {
        _currentPage = page;
    }

    private void ToggleOrganizationFilter() => _isOrganizationOpen = !_isOrganizationOpen;
    private void ToggleCategoryFilter() => _isCategoryOpen = !_isCategoryOpen;

    // Navigation
    private void NavigateToCreateEvent()
    {
        Navigation.NavigateTo("/organization/my");
    }

    private void NavigateToCreateOrganization()
    {
        Navigation.NavigateTo("/organization/create");
    }

    /// <summary>
    /// Gets a color code based on event type for placeholder images.
    /// </summary>
    private static string GetEventColorCode(EventListDto evt)
    {
        return evt.EventTypeFullName?.ToLower() switch
        {
            var s when s?.Contains("conference") == true => "2196F3",
            var s when s?.Contains("workshop") == true => "FF9800",
            var s when s?.Contains("webinar") == true => "4CAF50",
            var s when s?.Contains("seminar") == true => "E91E63",
            var s when s?.Contains("training") == true => "9C27B0",
            _ => "607D8B"
        };
    }

    /// <summary>
    /// Gets the image URL for an event (either the presigned URL or a placeholder).
    /// </summary>
    private static string GetEventImageUrl(EventListDto evt)
    {
        if (!string.IsNullOrEmpty(evt.FeaturedImageUri))
            return evt.FeaturedImageUri;

        // Generate placeholder with event title and color
        var encodedTitle = Uri.EscapeDataString(evt.Title.Length > 30
            ? evt.Title.Substring(0, 30) + "..."
            : evt.Title);
        var color = GetEventColorCode(evt);
        return $"https://placehold.co/600x400/{color}/ffffff?text={encodedTitle}";
    }

    /// <summary>
    /// Gets organization name for dropdown display.
    /// </summary>
    private string GetSelectedOrganizationName()
    {
        if (_selectedOrganizationId == Guid.Empty) return "All Organizations";
        return _myOrganizations.FirstOrDefault(o => o.Id == _selectedOrganizationId)?.FullName ?? "All Organizations";
    }

    /// <summary>
    /// Gets initials from a display name for avatar fallback.
    /// </summary>
    private static string GetActorInitials(string? displayName)
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
