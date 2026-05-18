// ABOUTME: Coordinates create/edit event-session save calls without owning Razor page lifecycle or UI state.
// ABOUTME: Keeps session save orchestration testable while preserving page-level validation, navigation, and submit UX.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Models.EventSessions;

namespace Explore.Blazor.Client.Pages.Events.Sessions;

internal static class EventSessionSaveCoordinator
{
    public static async Task<EventSessionSaveResult> SaveCreateSessionAsync(
        IEventService eventService,
        CreateEventSessionRequest session,
        Guid eventId,
        Guid? selectedSessionGroupId,
        Guid? savedSessionId)
    {
        ArgumentNullException.ThrowIfNull(eventService);
        ArgumentNullException.ThrowIfNull(session);

        var sessionId = savedSessionId;
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            var result = await eventService.CreateSessionAsync(session);
            if (result.Success != true || !result.Id.HasValue || result.Id.Value == Guid.Empty)
            {
                return EventSessionSaveResult.Failed(
                    result.Message ?? "Program item could not be saved. Check the details and try again.");
            }

            sessionId = result.Id.Value;
        }

        return await AssignSelectedProgramSectionAsync(
            eventService,
            eventId,
            selectedSessionGroupId,
            sessionId.Value,
            "Session was saved, but it could not be assigned to the selected program section.");
    }

    public static async Task<EventSessionSaveResult> SaveUpdateSessionAsync(
        IEventService eventService,
        UpdateEventSessionRequest session,
        Guid eventId,
        Guid sessionId,
        Guid? selectedSessionGroupId,
        Guid? initialSessionGroupId)
    {
        ArgumentNullException.ThrowIfNull(eventService);
        ArgumentNullException.ThrowIfNull(session);

        var result = await eventService.UpdateSessionAsync(session);
        if (result.Success != true)
        {
            return EventSessionSaveResult.Failed(
                result.Message ?? "Program item could not be saved. Check the details and try again.");
        }

        if (!selectedSessionGroupId.HasValue || selectedSessionGroupId.Value == Guid.Empty)
        {
            if (!initialSessionGroupId.HasValue || initialSessionGroupId.Value == Guid.Empty)
                return EventSessionSaveResult.Succeeded(sessionId);

            var unassignResult = await eventService.UnassignSessionFromGroupAsync(
                eventId,
                initialSessionGroupId.Value,
                sessionId);

            return unassignResult.Success == true
                ? EventSessionSaveResult.Succeeded(sessionId)
                : EventSessionSaveResult.Failed(
                    unassignResult.Message ?? "Session was saved, but it could not be removed from the previous program section.");
        }

        if (selectedSessionGroupId == initialSessionGroupId)
            return EventSessionSaveResult.Succeeded(sessionId);

        return await AssignSelectedProgramSectionAsync(
            eventService,
            eventId,
            selectedSessionGroupId,
            sessionId,
            "Session was saved, but it could not be assigned to the selected program section.");
    }

    private static async Task<EventSessionSaveResult> AssignSelectedProgramSectionAsync(
        IEventService eventService,
        Guid eventId,
        Guid? selectedSessionGroupId,
        Guid sessionId,
        string fallbackErrorMessage)
    {
        if (!selectedSessionGroupId.HasValue || selectedSessionGroupId.Value == Guid.Empty)
            return EventSessionSaveResult.Succeeded(sessionId);

        var result = await eventService.AssignSessionToGroupAsync(
            eventId,
            selectedSessionGroupId.Value,
            sessionId);

        return result.Success == true
            ? EventSessionSaveResult.Succeeded(sessionId)
            : EventSessionSaveResult.Failed(result.Message ?? fallbackErrorMessage, sessionId);
    }
}

internal sealed record EventSessionSaveResult(Guid? SessionId, string? ErrorMessage)
{
    public bool Success => ErrorMessage is null;

    public static EventSessionSaveResult Succeeded(Guid sessionId) => new(sessionId, null);

    public static EventSessionSaveResult Failed(string errorMessage, Guid? sessionId = null) => new(sessionId, errorMessage);
}
