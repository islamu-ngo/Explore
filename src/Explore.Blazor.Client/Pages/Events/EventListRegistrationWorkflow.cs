// ABOUTME: Page-local registration workflow helpers for EventList inline registration state transitions.
// ABOUTME: Keeps DTO construction and session-selection decisions out of the EventList component body.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

internal static class EventListRegistrationWorkflow
{
    private const string ContactShareConsentUiVersion = "v1";
    private const string DefaultFailureMessage = "Registration failed. Please try again.";

    public static EventListRegistrationLookup BuildRegistrationLookup(IEnumerable<EventRegistrationListDto>? registrations)
    {
        var registeredEventIds = new HashSet<Guid>();
        var registrationIdByEventId = new Dictionary<Guid, Guid>();

        if (registrations is null)
        {
            return new EventListRegistrationLookup(registeredEventIds, registrationIdByEventId);
        }

        foreach (var registration in registrations)
        {
            if (registration.EventId.HasValue && registration.Id.HasValue)
            {
                registeredEventIds.Add(registration.EventId.Value);
                registrationIdByEventId[registration.EventId.Value] = registration.Id.Value;
            }
        }

        return new EventListRegistrationLookup(registeredEventIds, registrationIdByEventId);
    }

    public static bool AreAllSessionsSelected(
        ICollection<EventSessionListDto>? availableSessions,
        IReadOnlySet<Guid> selectedSessionIds)
    {
        ArgumentNullException.ThrowIfNull(selectedSessionIds);

        var selectableSessionIds = GetSelectableSessionIds(availableSessions);
        return selectableSessionIds.Count > 0 && selectableSessionIds.SetEquals(selectedSessionIds);
    }

    public static HashSet<Guid> GetSelectableSessionIds(ICollection<EventSessionListDto>? availableSessions)
    {
        return availableSessions?
            .Where(session => session.Id.HasValue)
            .Select(session => session.Id!.Value)
            .ToHashSet() ?? [];
    }

    public static HashSet<Guid> ToggleSession(IReadOnlySet<Guid> selectedSessionIds, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(selectedSessionIds);

        var nextSelection = selectedSessionIds.ToHashSet();
        if (!nextSelection.Remove(sessionId))
        {
            nextSelection.Add(sessionId);
        }

        return nextSelection;
    }

    public static HashSet<Guid> ToggleAllSessions(
        ICollection<EventSessionListDto>? availableSessions,
        IReadOnlySet<Guid> selectedSessionIds)
    {
        ArgumentNullException.ThrowIfNull(selectedSessionIds);

        var selectableSessionIds = GetSelectableSessionIds(availableSessions);
        return AreAllSessionsSelected(availableSessions, selectedSessionIds)
            ? []
            : selectableSessionIds;
    }

    public static CreateEventRegistrationDto BuildSessionRegistrationRequest(
        Guid eventId,
        Guid? userId,
        IReadOnlyCollection<Guid> selectedSessionIds,
        bool shareEmailWithOrganizer,
        string organizerName)
    {
        ArgumentNullException.ThrowIfNull(selectedSessionIds);

        var request = new CreateEventRegistrationDto
        {
            EventId = eventId,
            UserId = userId,
            RegistrationScopeId = RegistrationPolicyHelper.ScopeSessionSelection,
            SelectedSessionIds = selectedSessionIds.ToList()
        };

        ApplyConsent(request, shareEmailWithOrganizer, organizerName);
        return request;
    }

    public static CreateEventRegistrationDto BuildRegistrationRequest(
        Guid eventId,
        Guid? userId,
        ICollection<EventSessionListDto>? availableSessions,
        IReadOnlyCollection<Guid> selectedSessionIds,
        int? registrationPolicyId,
        bool shareEmailWithOrganizer,
        string organizerName)
    {
        ArgumentNullException.ThrowIfNull(selectedSessionIds);

        var allowedScopes = RegistrationPolicyHelper.GetAllowedScopes(registrationPolicyId);
        var selectedSessionIdSet = selectedSessionIds.ToHashSet();

        if (!allowedScopes.Contains(RegistrationPolicyHelper.ScopeSessionSelection)
            && allowedScopes.Contains(RegistrationPolicyHelper.ScopeEvent)
            && AreAllSessionsSelected(availableSessions, selectedSessionIdSet))
        {
            var eventRequest = new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = userId,
                RegistrationScopeId = RegistrationPolicyHelper.ScopeEvent,
                SelectedSessionIds = null,
                SelectedEventDayId = null
            };

            ApplyConsent(eventRequest, shareEmailWithOrganizer, organizerName);
            return eventRequest;
        }

        return BuildSessionRegistrationRequest(
            eventId,
            userId,
            selectedSessionIds,
            shareEmailWithOrganizer,
            organizerName);
    }

    public static string BuildContactShareConsentText(string organizerName)
    {
        var displayName = string.IsNullOrWhiteSpace(organizerName)
            ? "the organizer"
            : organizerName.Trim();

        return $"Share my email address with {displayName} so they can contact me about future events and related updates.";
    }

    private static void ApplyConsent(
        CreateEventRegistrationDto request,
        bool shareEmailWithOrganizer,
        string organizerName)
    {
        request.ShareEmailWithOrganizer = shareEmailWithOrganizer;

        if (!shareEmailWithOrganizer)
        {
            return;
        }

        request.ConsentTextAcknowledged = BuildContactShareConsentText(organizerName);
        request.ConsentUiVersion = ContactShareConsentUiVersion;
    }

    public static bool IsWaitlistResponse(string? message)
    {
        return message?.Contains("waitlist", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool IsAlreadyRegisteredResponse(string? message)
    {
        return message?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true
            || message?.Contains("already registered", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static EventRegistrationOutcome ResolveOutcome(BaseCommandResponseOfGuid? response)
    {
        if (response?.Success == true)
        {
            if (IsAlreadyRegisteredResponse(response.Message))
            {
                return new EventRegistrationOutcome(
                    EventRegistrationOutcomeKind.AlreadyRegistered,
                    "Already Registered",
                    "You are already registered for this event.",
                    "You are already registered for this event.",
                    Severity.Info);
            }

            if (IsWaitlistResponse(response.Message))
            {
                return new EventRegistrationOutcome(
                    EventRegistrationOutcomeKind.Waitlisted,
                    "You're on the Waitlist!",
                    "You have been added to the waitlist because one or more selected sessions are currently full.",
                    "You have been added to the waitlist.",
                    Severity.Info);
            }

            return new EventRegistrationOutcome(
                EventRegistrationOutcomeKind.Confirmed,
                "You're Registered!",
                "You have been successfully registered for this event.",
                "Successfully registered!",
                Severity.Success);
        }

        var failureMessage = GetRegistrationFailureMessage(response);
        return new EventRegistrationOutcome(
            EventRegistrationOutcomeKind.Failed,
            "Registration Failed",
            failureMessage,
            failureMessage,
            Severity.Warning);
    }

    public static string GetRegistrationFailureMessage(BaseCommandResponseOfGuid? response)
    {
        return response?.Errors?.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
            ?? response?.Message
            ?? DefaultFailureMessage;
    }
}

internal sealed record EventListRegistrationLookup(
    HashSet<Guid> RegisteredEventIds,
    Dictionary<Guid, Guid> RegistrationIdByEventId);

internal enum EventRegistrationOutcomeKind
{
    Confirmed,
    Waitlisted,
    AlreadyRegistered,
    Failed
}

internal sealed record EventRegistrationOutcome(
    EventRegistrationOutcomeKind Kind,
    string Title,
    string Message,
    string SnackbarMessage,
    Severity SnackbarSeverity)
{
    public bool IsSuccessful => Kind != EventRegistrationOutcomeKind.Failed;
    public bool IsWaitlisted => Kind == EventRegistrationOutcomeKind.Waitlisted;
    public bool IsAlreadyRegistered => Kind == EventRegistrationOutcomeKind.AlreadyRegistered;
}
