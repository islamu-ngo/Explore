using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Event;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Contracts;
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
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IEventAspectService EventAspectService { get; set; } = default!;

    // Changed from private to protected to be accessible by Razor view
    [Inject] protected ILogger<EventDetail> Logger { get; set; } = default!;

    private Guid EventId { get; set; }

    private EventDto? _eventDetails;
    private ICollection<EventSessionListDto>? _eventSessions;
    private EventSessionListDto? _primarySession;
    private bool _isLoading = true;
    private bool _isUserRegistered;
    private bool _isCheckingRegistration = true;
    private bool _isCancellingRegistration = false;
    private List<Guid> _userRegistrationIds = new();
    private bool _canDelete = false;
    private bool _canEdit = false;
    private bool _isCheckingAuth = true;
    private string? _errorMessage;

    // Event Aspects
    private EventIslamicAspectDto? _islamicAspect;
    private EventTechAspectDto? _techAspect;
    private EventAppearanceSettings _appearance = new();

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
                _appearance = EventAppearanceMetadataHelper.Parse(_eventDetails.MetadataJson);

                // Load event sessions
                _eventSessions = await EventService.GetSessionsByEventAsync(EventId);
                _primarySession = _eventSessions?.FirstOrDefault();
                Logger.LogInformation("Loaded {SessionCount} sessions", _eventSessions?.Count ?? 0);

                // Check registration status, authorization, and load aspects in parallel
                var registrationTask = CheckRegistrationStatusAsync();
                var authTask = CheckDeleteAuthorizationAsync();
                var aspectsTask = LoadEventAspectsAsync();
                await Task.WhenAll(registrationTask, authTask, aspectsTask);
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

    /// <summary>
    /// Loads event aspects (Islamic and Tech) for the current event.
    /// </summary>
    private async Task LoadEventAspectsAsync()
    {
        try
        {
            var islamicTask = EventAspectService.GetIslamicAspectAsync(EventId);
            var techTask = EventAspectService.GetTechAspectAsync(EventId);

            await Task.WhenAll(islamicTask, techTask);

            _islamicAspect = await islamicTask;
            _techAspect = await techTask;

            Logger.LogDebug("Loaded aspects for event {EventId}: Islamic={HasIslamic}, Tech={HasTech}",
                EventId, _islamicAspect != null, _techAspect != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading event aspects for event {EventId}", EventId);
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
                if (user?.Id != null)
                {
                    var registrations = await EventService.GetRegistrationsByUserAsync(user.Id.Value);
                    if (_eventSessions != null && registrations != null)
                    {
                        var sessionIds = _eventSessions.Select(s => s.Id).ToHashSet();
                        var matchingRegistrations = registrations
                            .Where(r => sessionIds.Contains(r.EventSessionId) && r.Id.HasValue)
                            .ToList();
                        _isUserRegistered = matchingRegistrations.Any();
                        _userRegistrationIds = matchingRegistrations.Select(r => r.Id!.Value).ToList();
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
    /// Cancels the user's registration(s) for this event.
    /// </summary>
    private async Task CancelRegistrationAsync()
    {
        if (!_userRegistrationIds.Any()) return;

        var confirm = await DialogService.ShowMessageBox(
            "Cancel Registration",
            $"Are you sure you want to cancel your registration for \"{_eventDetails?.Title}\"?",
            yesText: "Cancel Registration",
            cancelText: "Keep Registration");

        if (confirm != true) return;

        _isCancellingRegistration = true;

        try
        {
            var allCancelled = true;
            foreach (var registrationId in _userRegistrationIds)
            {
                var success = await EventService.CancelEventRegistrationAsync(registrationId);
                if (!success) allCancelled = false;
            }

            if (allCancelled)
            {
                _isUserRegistered = false;
                _userRegistrationIds.Clear();
                Logger.LogInformation("Registration cancelled for event {EventId}", EventId);
            }
            else
            {
                _errorMessage = "Some registrations could not be cancelled. Please try again.";
                // Refresh to get accurate state
                await CheckRegistrationStatusAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cancelling registration for event {EventId}", EventId);
            _errorMessage = "An error occurred while cancelling registration.";
        }
        finally
        {
            _isCancellingRegistration = false;
        }
    }

    /// <summary>
    /// Checks if the current user is authorized to edit/delete this event.
    /// </summary>
    private async Task CheckDeleteAuthorizationAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                _canDelete = await EventService.CanDeleteEventAsync(EventId);
                // Edit permissions follow the same rules as delete (owner or admin)
                _canEdit = _canDelete;
                Logger.LogDebug("Authorization check for event {EventId}: CanDelete={CanDelete}, CanEdit={CanEdit}", EventId, _canDelete, _canEdit);
            }
            else
            {
                _canDelete = false;
                _canEdit = false;
                Logger.LogDebug("User not authenticated, cannot edit/delete event {EventId}", EventId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking authorization for event {EventId}", EventId);
            _canDelete = false;
            _canEdit = false;
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
        if (_primarySession != null && _primarySession.StartTime.HasValue && _primarySession.EndTime.HasValue)
        {
            var start = _primarySession.StartTime.Value.LocalDateTime;
            var end = _primarySession.EndTime.Value.LocalDateTime;

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
        return EventColorHelper.GetColorByTypeName(_eventDetails?.EventTypeFullName);
    }

    private string GetHeroStyle()
    {
        return EventAppearanceMetadataHelper.BuildHeroStyle(_appearance, $"#{GetEventColor()}");
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

            // For single session flow, we update status here too
            if (_eventSessions != null && _eventSessions.Count == 1)
            {
                await CheckRegistrationStatusAsync();
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
        if (_isCancellingRegistration) return "Cancelling...";
        if (_isUserRegistered) return "Already Registered";
        if (_primarySession == null) return "Registration unavailable";
        return _eventDetails?.IsRegistrationRequired == true ? "Register now" : "Join us";
    }

    /// <summary>
    /// Determines if the registration button should be disabled.
    /// </summary>
    private bool IsButtonDisabled()
    {
        return _isCheckingRegistration || _isCancellingRegistration || _isUserRegistered || _primarySession == null;
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

    #region Event Aspects Dialog Methods

    /// <summary>
    /// Opens the dialog to add a new Islamic aspect to the event.
    /// </summary>
    private async Task OpenAddIslamicAspectDialog()
    {
        await OpenIslamicAspectDialogAsync(existingAspect: null);
    }

    /// <summary>
    /// Opens the dialog to edit the existing Islamic aspect.
    /// </summary>
    private async Task OpenEditIslamicAspectDialog()
    {
        await OpenIslamicAspectDialogAsync(_islamicAspect);
    }

    /// <summary>
    /// Opens the Islamic aspect dialog for add or edit operations.
    /// </summary>
    private async Task OpenIslamicAspectDialogAsync(EventIslamicAspectDto? existingAspect)
    {
        var parameters = new DialogParameters
        {
            { "EventId", EventId },
            { "ExistingAspect", existingAspect }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<IslamicAspectEditDialog>(
            existingAspect == null ? "Add Islamic Characteristics" : "Edit Islamic Characteristics",
            parameters,
            options);

        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            // Reload the aspect to reflect changes
            await ReloadIslamicAspectAsync();
        }
    }

    /// <summary>
    /// Opens the dialog to add a new Tech aspect to the event.
    /// </summary>
    private async Task OpenAddTechAspectDialog()
    {
        await OpenTechAspectDialogAsync(existingAspect: null);
    }

    /// <summary>
    /// Opens the dialog to edit the existing Tech aspect.
    /// </summary>
    private async Task OpenEditTechAspectDialog()
    {
        await OpenTechAspectDialogAsync(_techAspect);
    }

    /// <summary>
    /// Opens the Tech aspect dialog for add or edit operations.
    /// </summary>
    private async Task OpenTechAspectDialogAsync(EventTechAspectDto? existingAspect)
    {
        var parameters = new DialogParameters
        {
            { "EventId", EventId },
            { "ExistingAspect", existingAspect }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<TechAspectEditDialog>(
            existingAspect == null ? "Add Tech Characteristics" : "Edit Tech Characteristics",
            parameters,
            options);

        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            // Reload the aspect to reflect changes
            await ReloadTechAspectAsync();
        }
    }

    /// <summary>
    /// Shows confirmation dialog and deletes the Islamic aspect if confirmed.
    /// </summary>
    private async Task ConfirmDeleteIslamicAspect()
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Delete Islamic Characteristics",
            "Are you sure you want to remove the Islamic characteristics from this event? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed == true)
        {
            try
            {
                var success = await EventAspectService.DeleteIslamicAspectAsync(EventId);
                if (success)
                {
                    _islamicAspect = null;
                }
                else
                {
                    _errorMessage = "Failed to remove Islamic characteristics";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting Islamic aspect for event {EventId}", EventId);
                _errorMessage = "An error occurred while removing Islamic characteristics";
            }
        }
    }

    /// <summary>
    /// Shows confirmation dialog and deletes the Tech aspect if confirmed.
    /// </summary>
    private async Task ConfirmDeleteTechAspect()
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Delete Tech Characteristics",
            "Are you sure you want to remove the Tech characteristics from this event? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed == true)
        {
            try
            {
                var success = await EventAspectService.DeleteTechAspectAsync(EventId);
                if (success)
                {
                    _techAspect = null;
                }
                else
                {
                    _errorMessage = "Failed to remove Tech characteristics";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting Tech aspect for event {EventId}", EventId);
                _errorMessage = "An error occurred while removing Tech characteristics";
            }
        }
    }

    /// <summary>
    /// Reloads only the Islamic aspect after add/edit operations.
    /// </summary>
    private async Task ReloadIslamicAspectAsync()
    {
        try
        {
            _islamicAspect = await EventAspectService.GetIslamicAspectAsync(EventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reloading Islamic aspect for event {EventId}", EventId);
        }
    }

    /// <summary>
    /// Reloads only the Tech aspect after add/edit operations.
    /// </summary>
    private async Task ReloadTechAspectAsync()
    {
        try
        {
            _techAspect = await EventAspectService.GetTechAspectAsync(EventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reloading Tech aspect for event {EventId}", EventId);
        }
    }

    #endregion
}
