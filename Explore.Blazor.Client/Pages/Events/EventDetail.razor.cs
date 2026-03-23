// ABOUTME: Event detail page logic for loading event data, sessions, and registration status.
// ABOUTME: Manages dialogs and persistent state for SEO-friendly prerender hydration.

using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Dialogs;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

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
    [Inject] private IEventSessionAgendaItemService AgendaItemService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [PersistentState]
    public EventDetailState? PersistedState { get; set; }

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
    private bool _isAuthenticated = false;
    private bool _isCheckingAuth = true;
    private string? _errorMessage;

    // Event Aspects
    private EventIslamicAspectDto? _islamicAspect;
    private EventTechAspectDto? _techAspect;
    private ICollection<EventSessionAgendaItemListDto>? _agendaItems;
    private EventAppearanceSettings _appearance = new();

    // Tag/Category management
    private bool _showDetailTagCatPopup;
    private TagCategoryMode _detailTagCatMode;
    private IReadOnlyCollection<Guid> _detailTagCatInitialIds = Array.Empty<Guid>();

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

        if (TryRestoreState())
        {
            return;
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
                _appearance = EventAppearanceMetadataHelper.FromColumns(
                    _eventDetails.BackgroundColor, _eventDetails.BackgroundImageUri, _eventDetails.BackgroundEffect);

                // Load event sessions
                _eventSessions = await EventService.GetSessionsByEventAsync(EventId);
                _primarySession = _eventSessions?.FirstOrDefault();
                Logger.LogInformation("Loaded {SessionCount} sessions", _eventSessions?.Count ?? 0);

                // Check authorization from HAL links (synchronous — links are already in the response)
                CheckAuthorizationFromHalLinks();

                // Check registration status and load aspects in parallel
                var registrationTask = CheckRegistrationStatusAsync();
                var aspectsTask = LoadEventAspectsAsync();
                var agendaTask = _primarySession?.Id != null && _primarySession.Id != Guid.Empty
                    ? AgendaItemService.GetAgendaItemsBySessionAsync(_primarySession.Id.Value)
                    : Task.FromResult<ICollection<EventSessionAgendaItemListDto>>(new List<EventSessionAgendaItemListDto>());
                await Task.WhenAll(registrationTask, aspectsTask, agendaTask);
                _agendaItems = agendaTask.Result;
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

        if (_eventDetails != null)
        {
            PersistState();
        }
    }

    private bool TryRestoreState()
    {
        if (PersistedState == null || PersistedState.EventId != EventId)
        {
            return false;
        }

        _eventDetails = PersistedState.EventDetails;
        _eventSessions = PersistedState.EventSessions;
        _primarySession = PersistedState.PrimarySession;
        _islamicAspect = PersistedState.IslamicAspect;
        _techAspect = PersistedState.TechAspect;
        _appearance = PersistedState.Appearance ?? new EventAppearanceSettings();
        _isLoading = false;
        _isCheckingRegistration = true;
        _isCheckingAuth = true;

        CheckAuthorizationFromHalLinks();
        _isCheckingAuth = false;

        _ = InvokeAsync(async () =>
        {
            await CheckRegistrationStatusAsync();
            _isCheckingRegistration = false;
            StateHasChanged();
        });

        return true;
    }

    private void PersistState()
    {
        PersistedState = new EventDetailState
        {
            EventId = EventId,
            EventDetails = _eventDetails,
            EventSessions = _eventSessions?.ToList() ?? new List<EventSessionListDto>(),
            PrimarySession = _primarySession,
            IslamicAspect = _islamicAspect,
            TechAspect = _techAspect,
            Appearance = _appearance
        };
    }

    public sealed class EventDetailState
    {
        public Guid EventId { get; init; }
        public EventDto? EventDetails { get; init; }
        public List<EventSessionListDto> EventSessions { get; init; } = new();
        public EventSessionListDto? PrimarySession { get; init; }
        public EventIslamicAspectDto? IslamicAspect { get; init; }
        public EventTechAspectDto? TechAspect { get; init; }
        public EventAppearanceSettings? Appearance { get; init; }
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
            _isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
            if (_isAuthenticated)
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

        var confirm = await DialogService.ShowMessageBoxAsync(
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
    /// Checks if the current user is authorized to edit/delete this event
    /// by reading HAL links from the server response.
    /// </summary>
    private void CheckAuthorizationFromHalLinks()
    {
        if (_eventDetails == null)
        {
            _canEdit = false;
            _canDelete = false;
            return;
        }

        _canEdit = _eventDetails.HasHalLink("edit");
        _canDelete = _eventDetails.HasHalLink("delete");
        Logger.LogDebug("HAL link authorization for event {EventId}: CanEdit={CanEdit}, CanDelete={CanDelete}", EventId, _canEdit, _canDelete);
    }

    /// <summary>
    /// Gets the event type display name.
    /// </summary>
    private string GetEventTypeName()
    {
        return _eventDetails?.EventTypeFullName ?? "Event";
    }

    /// <summary>
    /// Maps the event status master code to a MudBlazor Color for chips.
    /// </summary>
    private Color GetStatusChipColor() => _eventDetails?.EventStatusMasterCode switch
    {
        "PUBLISHED" => Color.Success,
        "DRAFT" => Color.Default,
        "CANCELLED" => Color.Error,
        "COMPLETED" => Color.Info,
        "POSTPONED" => Color.Warning,
        _ => Color.Default
    };

    private bool IsCancelledEvent() =>
        _eventDetails?.EventStatusMasterCode == "CANCELLED";

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
        return _eventDetails?.ActorProfilePictureUri ?? string.Empty;
    }

    /// <summary>
    /// Returns the profile page URL for the organizer actor, or null if no public profile exists for that actor type.
    /// Organization (ActorTypeId=2) → /organization/profile/{id}.
    /// </summary>
    private string? GetOrganizerProfileUrl()
    {
        if (_eventDetails?.ActorId == null || _eventDetails.ActorTypeId == null) return null;
        return _eventDetails.ActorTypeId.Value switch
        {
            2 => $"/organization/profile/{_eventDetails.ActorId.Value}",  // Organization
            _ => null
        };
    }

    private void NavigateToOrganizer()
    {
        var url = GetOrganizerProfileUrl();
        if (url != null)
            Navigation.NavigateTo(url);
    }

    /// <summary>
    /// Opens the registration dialog for the event.
    /// Handles single vs multiple session scenarios.
    /// </summary>
    private async Task OpenRegistrationDialog()
    {
        if (_eventDetails == null) return;

        if (!_isAuthenticated)
        {
            Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}");
            return;
        }

        // Ensure sessions are loaded
        if (_eventSessions == null || !_eventSessions.Any())
        {
            await DialogService.ShowMessageBoxAsync(
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

            var dialog = await SessionSelectionDialog.ShowAsync(
                DialogService,
                "Select Session",
                parameters,
                options);

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

        var dialog = await DialogService.ShowAsync<EventRegistration>(
            "Register",
            parameters,
            options);

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
        if (IsCancelledEvent()) return "Event Cancelled";
        if (_isUserRegistered) return "Already Registered";
        if (!_isAuthenticated) return "Login to Register";
        if (_primarySession == null) return "Registration unavailable";
        return _eventDetails?.IsRegistrationRequired == true ? "Register now" : "Join us";
    }

    /// <summary>
    /// Determines if the registration button should be disabled.
    /// </summary>
    private bool IsButtonDisabled()
    {
        return _isCheckingRegistration || _isCancellingRegistration || _isUserRegistered || IsCancelledEvent() || _primarySession == null;
    }

    /// <summary>
    /// Gets the registration button color based on registration status.
    /// </summary>
    private Color GetButtonColor()
    {
        return _isUserRegistered ? Color.Success : Color.Primary;
    }

    #region OG Metadata Helpers

    private string GetCanonicalUrl()
    {
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        return $"{baseUri}/events/{EventId}";
    }

    private string GetMetaDescription()
    {
        if (string.IsNullOrWhiteSpace(_eventDetails?.Description))
            return $"{_eventDetails?.Title} — Event on ISLAMU Events";

        var plainText = _eventDetails.Description
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Trim();

        return plainText.Length > 200
            ? string.Concat(plainText.AsSpan(0, 197), "...")
            : plainText;
    }

    private string GetOgImageUrl()
    {
        if (_eventDetails?.FeaturedImageId != Guid.Empty && _eventDetails?.FeaturedImageId != null)
        {
            var baseUri = Navigation.BaseUri.TrimEnd('/');
            return $"{baseUri}/api/storageobject/{_eventDetails.FeaturedImageId}/public";
        }

        return string.Empty;
    }

    private static string TruncateTitle(string title)
    {
        if (title.Length <= 70)
            return title;

        var truncated = title[..67];
        var lastSpace = truncated.LastIndexOf(' ');
        return lastSpace > 0
            ? string.Concat(truncated.AsSpan(0, lastSpace), "...")
            : string.Concat(truncated, "...");
    }

    #endregion

    #region Share

    private async Task ShareEventAsync()
    {
        var url = GetCanonicalUrl();

        try
        {
            // Try Web Share API first (mobile browsers)
            var canShare = await JsRuntime.InvokeAsync<bool>("eval", "!!navigator.share");
            if (canShare)
            {
                await JsRuntime.InvokeVoidAsync("navigator.share", new
                {
                    title = _eventDetails?.Title ?? "Event",
                    url
                });
                return;
            }
        }
        catch
        {
            // Web Share API not available or user cancelled — fall through to clipboard
        }

        try
        {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", url);
            Snackbar.Add("Link copied to clipboard!", Severity.Success,
                options => options.VisibleStateDuration = 2000);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to copy event link to clipboard");
            Snackbar.Add("Could not copy link", Severity.Error);
        }
    }

    #endregion

    #region Calendar Integration

    private async Task AddToGoogleCalendarAsync()
    {
        if (_eventDetails is null || _primarySession is null)
            return;

        var start = _primarySession.StartTime!.Value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");
        var end = _primarySession.EndTime!.Value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");
        var title = Uri.EscapeDataString(_eventDetails.Title);
        var details = Uri.EscapeDataString(
            GetMetaDescription() + "\n\n" + GetCanonicalUrl());
        var location = Uri.EscapeDataString(_primarySession.LocationFullName ?? "");

        var url = $"https://calendar.google.com/calendar/r/eventedit?text={title}&dates={start}/{end}&details={details}&location={location}";

        await JsRuntime.InvokeVoidAsync("open", url, "_blank");
    }

    private async Task DownloadIcsFileAsync()
    {
        if (_eventDetails is null || _primarySession is null)
            return;

        var ics = GenerateIcsContent();
        var bytes = System.Text.Encoding.UTF8.GetBytes(ics);
        var base64 = Convert.ToBase64String(bytes);
        var fileName = SanitizeFileName(_eventDetails.Title) + ".ics";

        await JsRuntime.InvokeVoidAsync("eval",
            $"(() => {{ const a = document.createElement('a'); " +
            $"a.href = 'data:text/calendar;base64,{base64}'; " +
            $"a.download = '{fileName}'; " +
            $"document.body.appendChild(a); a.click(); document.body.removeChild(a); }})()");
    }

    private string GenerateIcsContent()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//ISLAMU//Event Platform//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("BEGIN:VEVENT");

        var now = DateTime.UtcNow;
        sb.AppendLine($"DTSTAMP:{now:yyyyMMdd'T'HHmmss'Z'}");
        sb.AppendLine($"UID:{EventId}@islamu.events");

        if (_primarySession is not null)
        {
            sb.AppendLine($"DTSTART:{_primarySession.StartTime!.Value.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}");
            sb.AppendLine($"DTEND:{_primarySession.EndTime!.Value.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}");

            if (!string.IsNullOrWhiteSpace(_primarySession.LocationFullName))
                sb.AppendLine(IcsFoldLine($"LOCATION:{IcsEscape(_primarySession.LocationFullName)}"));
        }

        sb.AppendLine(IcsFoldLine($"SUMMARY:{IcsEscape(_eventDetails!.Title)}"));

        var description = GetMetaDescription() + "\\n\\n" + GetCanonicalUrl();
        sb.AppendLine(IcsFoldLine($"DESCRIPTION:{IcsEscape(description)}"));
        sb.AppendLine(IcsFoldLine($"URL:{GetCanonicalUrl()}"));
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");

        return sb.ToString();
    }

    private static string IcsEscape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    /// <summary>
    /// Folds an ICS content line at 75 octets per RFC 5545 §3.1.
    /// Continuation lines begin with a single space character.
    /// </summary>
    private static string IcsFoldLine(string line)
    {
        const int maxOctets = 75;
        var bytes = System.Text.Encoding.UTF8.GetBytes(line);
        if (bytes.Length <= maxOctets)
            return line;

        var sb = new System.Text.StringBuilder();
        int offset = 0;
        bool first = true;

        while (offset < bytes.Length)
        {
            int limit = first ? maxOctets : maxOctets - 1; // account for leading space on continuation
            int end = Math.Min(offset + limit, bytes.Length);

            // Avoid splitting in the middle of a multi-byte UTF-8 character
            while (end < bytes.Length && end > offset && (bytes[end] & 0xC0) == 0x80)
                end--;

            if (!first)
                sb.Append("\r\n ");

            sb.Append(System.Text.Encoding.UTF8.GetString(bytes, offset, end - offset));
            offset = end;
            first = false;
        }

        return sb.ToString();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalid.Contains(c))).Trim();
    }

    #endregion

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

        var dialog = await DeleteEventDialog.ShowAsync(
            DialogService,
            "Delete Event",
            parameters,
            options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            // Dialog already handled deletion and snackbar notification
            // Navigate to My Events page
            Navigation.NavigateTo("/my/events");
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

        var dialog = await IslamicAspectEditDialog.ShowAsync(
            DialogService,
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

        var dialog = await TechAspectEditDialog.ShowAsync(
            DialogService,
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
        var confirmed = await DialogService.ShowMessageBoxAsync(
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
        var confirmed = await DialogService.ShowMessageBoxAsync(
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

    // ── Tag/Category management ──

    private void OpenDetailTagManagement()
    {
        _detailTagCatMode = TagCategoryMode.Tags;
        _detailTagCatInitialIds = (_eventDetails?.Tags?
            .Where(t => t.Id.HasValue)
            .Select(t => t.Id!.Value)
            .ToList() ?? new List<Guid>()).AsReadOnly();
        _showDetailTagCatPopup = true;
    }

    private void OpenDetailCategoryManagement()
    {
        _detailTagCatMode = TagCategoryMode.Categories;
        _detailTagCatInitialIds = (_eventDetails?.Categories?
            .Where(c => c.Id.HasValue)
            .Select(c => c.Id!.Value)
            .ToList() ?? new List<Guid>()).AsReadOnly();
        _showDetailTagCatPopup = true;
    }

    private async Task HandleDetailTagCatSaved(IReadOnlyCollection<Guid> newIds)
    {
        var label = _detailTagCatMode == TagCategoryMode.Tags ? "Tag" : "Category";
        Snackbar.Add($"{label} changes saved.", Severity.Success);

        try
        {
            var detail = await EventService.GetEventByIdAsync(EventId);
            if (detail != null)
            {
                _eventDetails = detail;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing event after {Label} changes", label);
        }
    }
}
