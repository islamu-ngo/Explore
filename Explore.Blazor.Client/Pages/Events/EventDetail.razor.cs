// ABOUTME: Event detail page logic for loading event data, sessions, and registration status.
// ABOUTME: Manages dialogs and persistent state for SEO-friendly prerender hydration.

using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Dialogs;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

/// <summary>
/// Code-behind for EventDetail page.
/// Displays detailed information about an event including sessions, registration, and organizer info.
/// </summary>
public partial class EventDetail : ComponentBase, IDisposable
{
    private const string MainContentAppearanceOwner = "EventDetail";

    [Inject] private MainContentAppearanceState MainContentAppearanceState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IMapsService MapsService { get; set; } = default!;
    [Inject] private RouterStateService RouterState { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IEventAspectService EventAspectService { get; set; } = default!;
    [Inject] private IEventSessionAgendaItemService AgendaItemService { get; set; } = default!;
    [Inject] private IEventAgendaItemService EventAgendaItemService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [PersistentState]
    public EventDetailState? PersistedState { get; set; }

    // Changed from private to protected to be accessible by Razor view
    [Inject] protected ILogger<EventDetail> Logger { get; set; } = default!;
    [Inject] private IAccessibilityFocusService AccessibilityFocusService { get; set; } = default!;
    [Inject] private IAccessibilityAnnouncerService AnnouncerService { get; set; } = default!;
    [Inject] private IEventDayService EventDayService { get; set; } = default!;

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
    private bool _canManageTeam = false;
    private bool _isAuthenticated = false;
    private bool _isCheckingAuth = true;
    private string? _errorMessage;
    private bool _imageLoadFailed;
    private Guid _lastRenderedEventId;
    private bool _wasLoading = true;

    private ICollection<EventDayListDto>? _eventDays;

    // Event Aspects
    private EventIslamicAspectDto? _islamicAspect;
    private EventTechAspectDto? _techAspect;
    private ICollection<EventSessionAgendaItemListDto>? _agendaItems;
    private ICollection<EventAgendaItemListDto>? _eventAgendaItems;
    private AppearanceSettings _appearance = new();

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
            PublishMainContentAppearance();
            return;
        }

        await LoadEventDataAsync();
    }

    /// <summary>
    /// Resets scroll position when navigating to a new event.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_lastRenderedEventId != EventId || (_wasLoading && !_isLoading))
        {
            _lastRenderedEventId = EventId;
            _wasLoading = _isLoading;
            try
            {
                await JsRuntime.InvokeVoidAsync("window.scrollTo", 0, 0);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to scroll to top on navigation");
            }
        }
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
        _imageLoadFailed = false;

        try
        {
            Logger.LogInformation("Loading event {EventId}", EventId);
            _eventDetails = await EventService.GetEventByIdAsync(EventId);

            if (_eventDetails != null)
            {
                Logger.LogInformation("Loaded event: {Title}", _eventDetails.Title);
                _appearance = new AppearanceSettings
                {
                    BackgroundColor = _eventDetails.BackgroundColor ?? string.Empty,
                    ImageUri = _eventDetails.BackgroundImageUri ?? string.Empty,
                    BackgroundEffect = _eventDetails.BackgroundEffect ?? "None"
                };
                PublishMainContentAppearance();
                _islamicAspect = MapIslamicAspect(_eventDetails.IslamicAspect);
                _techAspect = MapTechAspect(_eventDetails.TechAspect);

                // Load event sessions
                _eventSessions = await EventService.GetSessionsByEventAsync(EventId);
                _primarySession = _eventSessions?.FirstOrDefault();
                Logger.LogInformation("Loaded {SessionCount} sessions", _eventSessions?.Count ?? 0);

                // Check authorization from HAL links (synchronous — links are already in the response)
                CheckAuthorizationFromHalLinks();

                // Check registration status and load aspects in parallel
                var registrationTask = CheckRegistrationStatusAsync();
                var aspectsTask = NeedsAspectFallbackLoad()
                    ? LoadEventAspectsAsync()
                    : Task.CompletedTask;
                var daysTask = EventDayService.GetDaysByEventAsync(EventId);
                var eventAgendaTask = EventAgendaItemService.GetAgendaItemsByEventAsync(EventId);
                var agendaTask = _primarySession?.Id != null && _primarySession.Id != Guid.Empty
                    ? AgendaItemService.GetAgendaItemsBySessionAsync(_primarySession.Id.Value)
                    : Task.FromResult<ICollection<EventSessionAgendaItemListDto>>(new List<EventSessionAgendaItemListDto>());
                await Task.WhenAll(registrationTask, aspectsTask, daysTask, eventAgendaTask, agendaTask);
                _agendaItems = await agendaTask;
                _eventDays = await daysTask;
                _eventAgendaItems = await eventAgendaTask;
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

        if (_errorMessage != null)
            await AnnouncerService.AnnounceAssertiveAsync(_errorMessage);
        else if (_eventDetails == null)
            await AnnouncerService.AnnouncePoliteAsync("Event not found");

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
        _eventDays = PersistedState.EventDays;
        _eventAgendaItems = PersistedState.EventAgendaItems;
        _agendaItems = PersistedState.SessionAgendaItems;
        _appearance = PersistedState.Appearance ?? new AppearanceSettings();
        _imageLoadFailed = false;
        PublishMainContentAppearance();
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
            EventDays = _eventDays?.ToList() ?? new List<EventDayListDto>(),
            EventAgendaItems = _eventAgendaItems?.ToList() ?? new List<EventAgendaItemListDto>(),
            SessionAgendaItems = _agendaItems?.ToList() ?? new List<EventSessionAgendaItemListDto>(),
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
        public List<EventDayListDto> EventDays { get; init; } = new();
        public List<EventAgendaItemListDto> EventAgendaItems { get; init; } = new();
        public List<EventSessionAgendaItemListDto> SessionAgendaItems { get; init; } = new();
        public AppearanceSettings? Appearance { get; init; }
    }

    private static EventIslamicAspectDto? MapIslamicAspect(EventIslamicAspectDto? aspect) => aspect;

    private static EventTechAspectDto? MapTechAspect(EventTechAspectDto? aspect) => aspect;

    private bool NeedsAspectFallbackLoad()
    {
        return ShouldLoadIslamicAspectFallback() || ShouldLoadTechAspectFallback();
    }

    private bool ShouldLoadIslamicAspectFallback() =>
        _islamicAspect is null && HasAvailableAspect("Islamic");

    private bool ShouldLoadTechAspectFallback() =>
        _techAspect is null && HasAvailableAspect("Tech");

    private bool HasAvailableAspect(string aspectName) =>
        _eventDetails?.AvailableAspects?.Any(aspect =>
            string.Equals(aspect, aspectName, StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>
    /// Loads event aspects (Islamic and Tech) for the current event.
    /// </summary>
    private async Task LoadEventAspectsAsync()
    {
        if (ShouldLoadIslamicAspectFallback())
        {
            try
            {
                _islamicAspect = await EventAspectService.GetIslamicAspectAsync(EventId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading Islamic aspect for event {EventId}", EventId);
            }
        }

        if (ShouldLoadTechAspectFallback())
        {
            try
            {
                _techAspect = await EventAspectService.GetTechAspectAsync(EventId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading tech aspect for event {EventId}", EventId);
            }
        }

        Logger.LogDebug("Loaded aspects for event {EventId}: Islamic={HasIslamic}, Tech={HasTech}",
            EventId, _islamicAspect != null, _techAspect != null);
    }

    private async Task LoadEventAgendaAsync()
    {
        try
        {
            var daysTask = EventDayService.GetDaysByEventAsync(EventId);
            var itemsTask = EventAgendaItemService.GetAgendaItemsByEventAsync(EventId);
            await Task.WhenAll(daysTask, itemsTask);
            _eventDays = await daysTask;
            _eventAgendaItems = await itemsTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reloading event agenda for event {EventId}", EventId);
        }

        StateHasChanged();
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

        await AccessibilityFocusService.SaveFocusAsync();
        var confirm = await DialogService.ShowMessageBoxAsync(
            "Cancel Registration",
            $"Are you sure you want to cancel your registration for \"{_eventDetails?.Title}\"?",
            yesText: "Cancel Registration",
            cancelText: "Keep Registration");
        await AccessibilityFocusService.RestoreFocusAsync();

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
            _canManageTeam = false;
            return;
        }

        _canEdit = _eventDetails.HasHalLink("edit");
        _canDelete = _eventDetails.HasHalLink("delete");
        _canManageTeam = _eventDetails.HasHalLink("team");
        Logger.LogDebug("HAL link authorization for event {EventId}: CanEdit={CanEdit}, CanDelete={CanDelete}, CanManageTeam={CanManageTeam}", EventId, _canEdit, _canDelete, _canManageTeam);
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
        return AppearanceStyleBuilder.BuildHeroStyle(_appearance, $"#{GetEventColor()}");
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
    /// Organization (ActorTypeId=2) → /organization/profile/{id}; Group (ActorTypeId=4) → /group/profile/{id}.
    /// </summary>
    private string? GetOrganizerProfileUrl()
    {
        if (_eventDetails?.ActorId == null || _eventDetails.ActorTypeId == null) return null;
        return _eventDetails.ActorTypeId.Value switch
        {
            2 => $"/organization/profile/{_eventDetails.ActorId.Value}",  // Organization
            4 => $"/group/profile/{_eventDetails.ActorId.Value}",          // Group
            _ => null
        };
    }

    private void NavigateToOrganizer()
    {
        var url = GetOrganizerProfileUrl();
        if (url != null)
            Navigation.NavigateTo(url);
    }

    private void HandleOrganizerKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " ")
        {
            NavigateToOrganizer();
        }
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
            await AccessibilityFocusService.SaveFocusAsync();
            await LoginPromptDialog.ShowAsync(
                DialogService,
                new Uri(Navigation.Uri).PathAndQuery,
                "Sign in to register for this event. After you sign in, we will bring you back here to finish registration.");
            await AccessibilityFocusService.RestoreFocusAsync();
            return;
        }

        var allowedScopes = RegistrationPolicyHelper.GetAllowedScopes(_eventDetails.RegistrationPolicyId);

        var needsSessions = allowedScopes.Contains(RegistrationPolicyHelper.ScopeSessionSelection);
        if (needsSessions && (_eventSessions == null || !_eventSessions.Any()))
        {
            var onlySessionScope = allowedScopes.Count == 1;
            if (onlySessionScope)
            {
                await AccessibilityFocusService.SaveFocusAsync();
                await DialogService.ShowMessageBoxAsync(
                    "Registration unavailable",
                    "No sessions are available for this event yet.",
                    yesText: "OK");
                await AccessibilityFocusService.RestoreFocusAsync();
                return;
            }
        }

        var needsDays = allowedScopes.Contains(RegistrationPolicyHelper.ScopeDay);
        if (needsDays && (_eventDays == null || !_eventDays.Any()) && !allowedScopes.Contains(RegistrationPolicyHelper.ScopeEvent))
        {
            await AccessibilityFocusService.SaveFocusAsync();
            await DialogService.ShowMessageBoxAsync(
                "Registration unavailable",
                "No event days are available for this event yet.",
                yesText: "OK");
            await AccessibilityFocusService.RestoreFocusAsync();
            return;
        }

        await AccessibilityFocusService.SaveFocusAsync();

        var parameters = new DialogParameters
        {
            { "EventId", _eventDetails.Id },
            { "Title", $"Register for {_eventDetails.Title}" },
            { "RegistrationPolicyId", _eventDetails.RegistrationPolicyId },
            { "Days", _eventDays },
            { "Sessions", _eventSessions },
            { "RecipientActorId", _eventDetails.ActorId },
            { "PublisherOrganizationName", _eventDetails.ActorDisplayName }
        };

        var options = DialogOptionsFactory.Medium();

        var dialog = await DialogService.ShowAsync<EventRegistration>(
            "Register",
            parameters,
            options);

        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            await CheckRegistrationStatusAsync();
        }

        await AccessibilityFocusService.RestoreFocusAsync();
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
        if (!HasAvailableRegistrationTarget()) return "Registration unavailable";
        return _eventDetails?.IsRegistrationRequired == true ? "Register now" : "Join us";
    }

    /// <summary>
    /// Determines if the registration button should be disabled.
    /// </summary>
    private bool IsButtonDisabled()
    {
        return _isCheckingRegistration
            || _isCancellingRegistration
            || _isUserRegistered
            || IsCancelledEvent()
            || !HasAvailableRegistrationTarget();
    }

    /// <summary>
    /// Gets the registration button color based on registration status.
    /// </summary>
    private Color GetButtonColor()
    {
        return _isUserRegistered ? Color.Success : Color.Primary;
    }

    private bool HasAvailableRegistrationTarget()
    {
        if (_eventDetails is null)
        {
            return false;
        }

        var allowedScopes = RegistrationPolicyHelper.GetAllowedScopes(_eventDetails.RegistrationPolicyId);
        return allowedScopes.Contains(RegistrationPolicyHelper.ScopeEvent)
            || (allowedScopes.Contains(RegistrationPolicyHelper.ScopeDay) && _eventDays?.Any() == true)
            || (allowedScopes.Contains(RegistrationPolicyHelper.ScopeSessionSelection) && _eventSessions?.Any() == true);
    }

    #region OG Metadata Helpers

    private string GetCanonicalUrl()
    {
        return CanonicalUrlHelper.Build(Navigation, $"/events/{EventId}");
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

    private string GetAboutText()
    {
        if (!string.IsNullOrWhiteSpace(_eventDetails?.Content))
            return _eventDetails.Content;

        return string.IsNullOrWhiteSpace(_eventDetails?.Description)
            ? "No event content available."
            : _eventDetails.Description;
    }

    private string GetOgImageUrl()
    {
        return GetFeaturedImagePublicUrl() ?? string.Empty;
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

    private string GetCalendarDownloadUrl()
    {
        return EventId == Guid.Empty
            ? "#"
            : $"/api/event/{EventId}/calendar";
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

        var options = DialogOptionsFactory.Small();

        await AccessibilityFocusService.SaveFocusAsync();
        var dialog = await DeleteEventDialog.ShowAsync(
            DialogService,
            "Delete Event",
            parameters,
            options);
        var result = await dialog.Result;
        await AccessibilityFocusService.RestoreFocusAsync();

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

        var options = DialogOptionsFactory.Medium();

        await AccessibilityFocusService.SaveFocusAsync();
        var dialog = await IslamicAspectEditDialog.ShowAsync(
            DialogService,
            existingAspect == null ? "Add Islamic Characteristics" : "Edit Islamic Characteristics",
            parameters,
            options);

        var result = await dialog.Result;
        await AccessibilityFocusService.RestoreFocusAsync();

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

        var options = DialogOptionsFactory.Medium();

        await AccessibilityFocusService.SaveFocusAsync();
        var dialog = await TechAspectEditDialog.ShowAsync(
            DialogService,
            existingAspect == null ? "Add Tech Characteristics" : "Edit Tech Characteristics",
            parameters,
            options);

        var result = await dialog.Result;
        await AccessibilityFocusService.RestoreFocusAsync();

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
        await AccessibilityFocusService.SaveFocusAsync();
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Islamic Characteristics",
            "Are you sure you want to remove the Islamic characteristics from this event? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");
        await AccessibilityFocusService.RestoreFocusAsync();

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
        await AccessibilityFocusService.SaveFocusAsync();
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Tech Characteristics",
            "Are you sure you want to remove the Tech characteristics from this event? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");
        await AccessibilityFocusService.RestoreFocusAsync();

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

    // ── UI Formatting Helpers ──

    private string GetWrapperStyle()
    {
        return _appearance.IsEmpty
            ? string.Empty
            : AppearanceStyleBuilder.BuildSurfaceStyle(_appearance, $"#{GetEventColor()}");
    }

    private string? GetImageUrl()
    {
        if (!string.IsNullOrEmpty(_eventDetails?.FeaturedImageUri))
        {
            return _eventDetails.FeaturedImageUri;
        }

        var publicFeaturedImageUrl = GetFeaturedImagePublicUrl();
        if (!string.IsNullOrWhiteSpace(publicFeaturedImageUrl))
        {
            return publicFeaturedImageUrl;
        }

        if (!string.IsNullOrWhiteSpace(_appearance.ImageUri))
        {
            return _appearance.ImageUri;
        }
        return null;
    }

    private string? GetFeaturedImagePublicUrl()
    {
        if (_eventDetails?.FeaturedImageId is not Guid imageId || imageId == Guid.Empty)
        {
            return null;
        }

        var baseUri = Navigation.BaseUri.TrimEnd('/');
        return $"{baseUri}/api/storageobject/{imageId}/public";
    }

    private string GetFallbackSvgDataUri()
    {
        return ImageHelper.GetEventImageUrl(null, _eventDetails?.Title ?? "Event", GetEventColor(), width: 300, height: 400);
    }

    private void PublishMainContentAppearance()
    {
        var style = _appearance.IsEmpty
            ? string.Empty
            : AppearanceStyleBuilder.BuildSurfaceStyle(_appearance, $"#{GetEventColor()}");
        MainContentAppearanceState.Set(MainContentAppearanceOwner, style);
    }

    private void HandleImageError()
    {
        _imageLoadFailed = true;
    }

    public void Dispose()
    {
        MainContentAppearanceState.Clear(MainContentAppearanceOwner);
    }

    private string GetDateMonth() => _eventDetails?.FirstSessionDate?.ToString("MMM") ?? "TBD";
    private string GetDateDay() => _eventDetails?.FirstSessionDate?.ToString("dd") ?? "--";
    private string GetDateFull() => _eventDetails?.FirstSessionDate?.ToString("dddd, MMMM d, yyyy") ?? "Date to be announced";

    private string GetTimeRange()
    {
        if (_eventDetails == null) return string.Empty;

        var start = _eventDetails.FirstSessionDate?.ToString("d") ?? "Schedule pending";
        if (_eventDetails.LastSessionDate.HasValue && _eventDetails.LastSessionDate.Value != _eventDetails.FirstSessionDate)
        {
            var end = _eventDetails.LastSessionDate.Value.ToString("d");
            return $"{start} - {end}";
        }
        return start;
    }

    private bool IsDigitalEvent() =>
        string.Equals(_eventDetails?.EventFormatMasterCode, "DIGITAL", StringComparison.OrdinalIgnoreCase);

    private bool IsHybridEvent() =>
        string.Equals(_eventDetails?.EventFormatMasterCode, "HYBRID", StringComparison.OrdinalIgnoreCase);

    private string GetLocationHint()
    {
        if (IsDigitalEvent())
        {
            return "Online event";
        }

        if (IsHybridEvent())
        {
            return "Hybrid event";
        }

        return string.IsNullOrWhiteSpace(GetFullLocation())
            ? "Location to be announced"
            : "Register to see any private address details";
    }

    private string GetAudienceDisplay()
    {
        var gender = string.IsNullOrWhiteSpace(_eventDetails?.AudienceGenderFullName)
            ? "All genders"
            : _eventDetails!.AudienceGenderFullName;
        var age = string.IsNullOrWhiteSpace(_eventDetails?.AudienceAgeFullName)
            ? "All ages"
            : _eventDetails!.AudienceAgeFullName;
        return $"{gender} · {age}";
    }

    private string GetPriceDisplay()
    {
        if (_eventDetails?.Price is > 0)
        {
            return $"{_eventDetails.CurrencyCode ?? "EUR"} {_eventDetails.Price:0.##}";
        }

        return "Free";
    }

    private string GetRegistrationPolicyDisplay()
    {
        if (!string.IsNullOrWhiteSpace(_eventDetails?.RegistrationPolicyFullName))
        {
            return _eventDetails.RegistrationPolicyFullName;
        }

        return _eventDetails?.IsRegistrationRequired == true
            ? "Registration required"
            : "Registration optional";
    }

    private string GetProgramSummary()
    {
        var count = _eventDetails?.SessionCount ?? _eventSessions?.Count ?? 0;
        return count switch
        {
            0 => "Program not published yet",
            1 => "1 program item",
            _ => $"{count} program items"
        };
    }
}
