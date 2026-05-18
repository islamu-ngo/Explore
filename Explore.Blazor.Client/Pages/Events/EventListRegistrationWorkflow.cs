// ABOUTME: Page-local registration workflow helpers for EventList inline registration state transitions.
// ABOUTME: Keeps DTO construction and session-selection decisions out of the EventList component body.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Pages.Events;

internal static class EventListRegistrationWorkflow
{
    private const int SessionSelectionRegistrationScopeId = 3;
    private const string ContactShareConsentUiVersion = "v1";

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
        return selectableSessionIds.Count > 0 && selectedSessionIds.Count == selectableSessionIds.Count;
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
            RegistrationScopeId = SessionSelectionRegistrationScopeId,
            SelectedSessionIds = selectedSessionIds.ToList()
        };

        if (shareEmailWithOrganizer)
        {
            request.ShareEmailWithOrganizer = true;
            request.ConsentTextAcknowledged = BuildContactShareConsentText(organizerName);
            request.ConsentUiVersion = ContactShareConsentUiVersion;
        }

        return request;
    }

    public static string BuildContactShareConsentText(string organizerName)
    {
        var displayName = string.IsNullOrWhiteSpace(organizerName)
            ? "the organizer"
            : organizerName.Trim();

        return $"Share my email address with {displayName} so they can contact me about future events and related updates.";
    }

    public static bool IsWaitlistResponse(string? message)
    {
        return message?.Contains("waitlist", StringComparison.OrdinalIgnoreCase) == true;
    }
}

internal sealed record EventListRegistrationLookup(
    HashSet<Guid> RegisteredEventIds,
    Dictionary<Guid, Guid> RegistrationIdByEventId);
