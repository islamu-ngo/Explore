using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Event;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Event;

/// <summary>
/// Code-behind for EventDetail page.
/// Displays detailed information about an event including sessions, registration, and organizer info.
/// </summary>
public partial class EventDetail : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IMapsService MapsService { get; set; } = default!;
    [Inject] private RouterStateService RouterState { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    
    // Changed from private to protected to be accessible by Razor view
    [Inject] protected ILogger<EventDetail> Logger { get; set; } = default!;

    private Guid EventId { get; set; }

    private EventDto? _eventDetails;
    private ICollection<EventSessionListDto>? _eventSessions;
    private EventSessionListDto? _primarySession;
    private bool _isLoading = true;
    private bool _isUserRegistered;
    private bool _isCheckingRegistration = true;
    private bool _canDelete = false;
    private bool _isCheckingAuth = true;
    private string? _errorMessage;

    /// <summary>
    /// Initializes the component and loads event data.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var eventIdStr = RouterState.GetParam("eventId");
        if (Guid.TryParse(eventIdStr, out var id))
        {
            EventId = id;
        }
        await LoadEventDataAsync();
    }

    /// <summary>
    /// Loads event details, sessions, and registration status.
    /// </summary>
    private async Task LoadEventDataAsync()
    {
        _isLoading = true;
        _isCheckingRegistration = true;
        _isCheckingAuth = true;
        _errorMessage = null;

        try
        {
            Logger.LogInformation("Loading event {EventId}", EventId);
            _eventDetails = await EventService.GetEventByIdAsync(EventId);

            if (_eventDetails != null)
            {
                Logger.LogInformation("Loaded event: {Title}", _eventDetails.Title);

                // Load event sessions
                _eventSessions = await EventService.GetSessionsByEventAsync(EventId);
                _primarySession = _eventSessions?.FirstOrDefault();
                Logger.LogInformation("Loaded {SessionCount} sessions", _eventSessions?.Count ?? 0);

                // Check registration status and authorization in parallel
                var registrationTask = CheckRegistrationStatusAsync();
                var authTask = CheckDeleteAuthorizationAsync();
                await Task.WhenAll(registrationTask, authTask);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load event details: {ex.Message}";
            Logger.LogError(ex, "Failed to load event {EventId}", EventId);
        }
        finally
        {
            _isLoading = false;
            _isCheckingRegistration = false;
            _isCheckingAuth = false;
        }
    }

    private async Task CheckRegistrationStatusAsync()
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
                    // Check if any registration belongs to any session of this event
                    if (_eventSessions != null && registrations != null)
                    {
                        var sessionIds = _eventSessions.Select(s => s.Id).ToHashSet();
                        _isUserRegistered = registrations.Any(r => sessionIds.Contains(r.EventSessionId));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking registration status");
        }
    }

    /// <summary>
    /// Checks if the current user is authorized to delete this event.
    /// </summary>
    private async Task CheckDeleteAuthorizationAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                _canDelete = await EventService.CanDeleteEventAsync(EventId);
                Logger.LogDebug("Delete authorization check for event {EventId}: {CanDelete}", EventId, _canDelete);
            }
            else
            {
                _canDelete = false;
                Logger.LogDebug("User not authenticated, cannot delete event {EventId}", EventId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking delete authorization for event {EventId}", EventId);
            _canDelete = false;
        }
    }

    /// <summary>
    /// Gets the event type display name.
    /// </summary>
    private string GetEventTypeName()
    {
        return _eventDetails?.EventTypeFullName ?? "Event";
    }

    /// <summary>
    /// Gets the formatted date display string.
    /// </summary>
    private string GetDateDisplay()
    {
        // Use session data if available, otherwise fall back to event dates
        if (_primarySession != null)
        {
            var start = _primarySession.StartTime.LocalDateTime;
            var end = _primarySession.EndTime.LocalDateTime;

            if (start.Date == end.Date)
            {
                return $"{start:dddd d MMMM yyyy} • {start:HH:mm} - {end:HH:mm}";
            }

            return $"{start:dd/MM/yyyy HH:mm} - {end:dd/MM/yyyy HH:mm}";
        }

        if (_eventDetails == null) return string.Empty;

        if (_eventDetails.FirstSessionDate.HasValue && _eventDetails.LastSessionDate.HasValue)
        {
            var start = _eventDetails.FirstSessionDate.Value.LocalDateTime;
            var end = _eventDetails.LastSessionDate.Value.LocalDateTime;

            if (start.Date == end.Date)
            {
                return $"{start:dddd d MMMM yyyy}";
            }

            return $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";
        }

        return "Date TBD";
    }

    /// <summary>
    /// Gets a short location display string.
    /// </summary>
    private string GetLocationDisplay()
    {
        if (_primarySession != null && !string.IsNullOrEmpty(_primarySession.LocationFullName))
        {
            return _primarySession.LocationFullName;
        }
        if (_primarySession != null && !string.IsNullOrEmpty(_primarySession.LocationCity))
        {
            return _primarySession.LocationCity;
        }

        return _eventDetails?.EventFormatFullName ?? "Online";
    }

    /// <summary>
    /// Gets the full location display string with address details.
    /// </summary>
    private string GetFullLocation()
    {
        if (_primarySession != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(_primarySession.LocationFullName))
                parts.Add(_primarySession.LocationFullName);
            if (!string.IsNullOrEmpty(_primarySession.LocationCity))
                parts.Add(_primarySession.LocationCity);

            if (parts.Count > 0)
                return string.Join(", ", parts);
        }

        return _eventDetails?.EventFormatFullName ?? "Location to be announced";
    }

    /// <summary>
    /// Generates a color code based on event type for placeholder images.
    /// </summary>
    private string GetEventColor()
    {
        if (_eventDetails == null) return "607D8B";

        return _eventDetails.EventTypeFullName?.ToLower() switch
        {
            var s when s?.Contains("conference") == true => "2196F3",
            var s when s?.Contains("workshop") == true => "FF9800",
            var s when s?.Contains("seminar") == true => "9C27B0",
            _ => "607D8B"
        };
    }

    /// <summary>
    /// Checks if the event is organized by an organization (vs a user).
    /// </summary>
    private bool IsOrganizedByOrganization()
    {
        return _eventDetails?.ActorTypeFullName?.Equals("Organization", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Gets the organizer display name from the actor info.
    /// </summary>
    private string GetOrganizerName()
    {
        return _eventDetails?.ActorDisplayName ?? "Unknown Organizer";
    }

    /// <summary>
    /// Gets the organizer profile picture URL.
    /// </summary>
    private string GetOrganizerProfilePicture()
    {
        return _eventDetails?.ActorProfilePictureUri ?? "https://via.placeholder.com/150";
    }

    /// <summary>
    /// Opens the registration dialog for the event.
    /// Handles single vs multiple session scenarios.
    /// </summary>
    private async Task OpenRegistrationDialog()
    {
        if (_eventDetails == null) return;
        
        // Ensure sessions are loaded
        if (_eventSessions == null || !_eventSessions.Any())
        {
            await DialogService.ShowMessageBox(
                "Registration unavailable",
                "No sessions are available for this event yet.",
                yesText: "OK");
            return;
        }

        // Case 1: Single Session -> Register directly
        if (_eventSessions.Count == 1)
        {
            await RegisterForSession(_eventSessions.First());
        }
        // Case 2: Multiple Sessions -> Show Selection Dialog
        else
        {
            var parameters = new DialogParameters
            {
                { "Sessions", _eventSessions.ToList() }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                Position = DialogPosition.Center
            };

            var dialog = await DialogService.ShowAsync<Explore.Blazor.Client.Components.Event.SessionSelectionDialog>(
                "Select Session",
                parameters,
                options
            );

            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is List<Guid> selectedSessionIds)
            {
                // Register for each selected session
                int successCount = 0;
                foreach (var sessionId in selectedSessionIds)
                {
                    var session = _eventSessions.First(s => s.Id == sessionId);
                    if (await RegisterForSession(session))
                    {
                        successCount++;
                    }
                }
                
                if (successCount > 0)
                {
                    // Refresh status to update UI immediately
                    await CheckRegistrationStatusAsync();
                    StateHasChanged();
                }
            }
        }
    }

    /// <summary>
    /// Registers the user for a specific session.
    /// </summary>
    private async Task<bool> RegisterForSession(EventSessionListDto session)
    {
        var parameters = new DialogParameters
        {
            { "EventSessionId", session.Id },
            { "Title", $"Register for {_eventDetails!.Title} - {session.Title}" }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            Position = DialogPosition.Center
        };

        var dialog = await DialogService.ShowAsync<Components.EventRegistration>(
            "Register",
            parameters,
            options
        );

        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Logger.LogInformation("Registration completed for session {SessionId}", session.Id);
            Snackbar.Add($"Successfully registered for {session.Title}!", Severity.Success);
            
            // For single session flow, we update status here too
            if (_eventSessions != null && _eventSessions.Count == 1)
            {
                await CheckRegistrationStatusAsync();
                StateHasChanged();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the registration button text based on current state.
    /// </summary>
    private string GetButtonText()
    {
        if (_isCheckingRegistration) return "Checking...";
        if (_isUserRegistered) return "Already Registered";
        if (_primarySession == null) return "Registration unavailable";
        return _eventDetails?.IsRegistrationRequired == true ? "Register now" : "Join us";
    }

    /// <summary>
    /// Determines if the registration button should be disabled.
    /// </summary>
    private bool IsButtonDisabled()
    {
        return _isCheckingRegistration || _isUserRegistered || _primarySession == null;
    }

    /// <summary>
    /// Gets the registration button color based on registration status.
    /// </summary>
    private Color GetButtonColor()
    {
        return _isUserRegistered ? Color.Success : Color.Primary;
    }

    /// <summary>
    /// Adds the event to Google Calendar.
    /// </summary>
    private void AddToGoogleCalendar()
    {
        Logger.LogInformation("Add to Google Calendar clicked for event {EventId}", EventId);
        // TODO: Implement Google Calendar integration
    }

    /// <summary>
    /// Generates and downloads an Apple Calendar (.ics) file.
    /// </summary>
    private void AddToAppleCalendar()
    {
        Logger.LogInformation("Add to Apple Calendar clicked for event {EventId}", EventId);
        // TODO: Implement ICS file generation
    }

    /// <summary>
    /// Generates and downloads an Outlook Calendar (.ics) file.
    /// </summary>
    private void AddToOutlookCalendar()
    {
        Logger.LogInformation("Add to Outlook Calendar clicked for event {EventId}", EventId);
        // TODO: Implement ICS file generation
    }

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    private async Task OpenDeleteDialog()
    {
        if (_eventDetails == null) return;

        var parameters = new DialogParameters
        {
            { "EventId", EventId },
            { "EventTitle", _eventDetails.Title }
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
            // Dialog already handled deletion and snackbar notification
            // Navigate to My Events page
            Navigation.NavigateTo("/myevents");
        }
    }
}
