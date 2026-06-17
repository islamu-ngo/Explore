// ABOUTME: Page-local mapper for dedicated program item create/edit composers.
// ABOUTME: Keeps session request normalization out of Razor components while preserving page-owned orchestration.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models.EventSessions;
using ComposerCreateEventSessionRequest = Explore.Blazor.Client.Models.EventSessions.CreateEventSessionRequest;

namespace Explore.Blazor.Client.Pages.Events.Sessions;

internal static class EventSessionFormModelMapper
{
    public static EventSessionCreateFormState ApplyCreateContext(
        ComposerCreateEventSessionRequest session,
        EventSessionCreateContextDto context,
        int fallbackRegistrationModeId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(context);

        session.RegistrationModeId = context.Defaults?.RegistrationModeId > 0
            ? context.Defaults.RegistrationModeId
            : fallbackRegistrationModeId;

        DateTime? sessionDate = null;
        if (context.Defaults?.SessionDate is { } defaultDate)
        {
            sessionDate = defaultDate.DateTime;
        }

        return new EventSessionCreateFormState(
            context.Locations ?? new List<EventSessionCreateLocationOptionDto>(),
            context.SessionGroups ?? new List<EventSessionCreateGroupOptionDto>(),
            sessionDate,
            TryParseDefaultTime(context.Defaults?.StartTime, out var startTime) ? startTime : null,
            TryParseDefaultTime(context.Defaults?.EndTime, out var endTime) ? endTime : null);
    }

    public static EventSessionEditFormState PopulateUpdateRequest(
        UpdateEventSessionRequest session,
        EventSessionDto sourceSession,
        Guid eventId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sourceSession);

        session.Id = sourceSession.Id;
        session.EventId = eventId;
        session.Title = sourceSession.Title ?? string.Empty;
        session.Description = sourceSession.Description;
        session.LocationId = sourceSession.LocationId;
        session.RoomId = sourceSession.RoomId;
        session.SortOrder = sourceSession.SortOrder;
        session.FeaturedImageId = sourceSession.FeaturedImageId;
        session.EventSessionKindId = sourceSession.EventSessionKindId;
        session.MaxAudienceAttendees = sourceSession.MaxAudienceAttendees;
        session.RegistrationModeId = sourceSession.RegistrationModeId;
        session.Slug = sourceSession.Slug;
        session.Price = sourceSession.Price;
        session.CurrencyCode = sourceSession.CurrencyCode;
        session.IslamicAspect = MapIslamicAspect(sourceSession.IslamicAspect);

        var localStart = DateTimeHelper.ConvertUtcToLocal(sourceSession.StartTime);
        var localEnd = DateTimeHelper.ConvertUtcToLocal(sourceSession.EndTime);
        return new EventSessionEditFormState(
            localStart?.Date ?? DateTime.Today,
            localStart?.TimeOfDay ?? new TimeSpan(9, 0, 0),
            localEnd?.TimeOfDay ?? new TimeSpan(10, 0, 0),
            GetPrimarySessionGroupId(sourceSession));
    }

    public static bool TryPrepareCreateRequest(
        ComposerCreateEventSessionRequest session,
        Guid eventId,
        Guid? tenantId,
        DateTime? sessionDate,
        TimeSpan? startTime,
        TimeSpan? endTime,
        out string? validationError)
    {
        ArgumentNullException.ThrowIfNull(session);
        validationError = null;

        if (tenantId is null || tenantId == Guid.Empty)
        {
            validationError = "The event draft is missing tenant context. Return to the draft and try again.";
            return false;
        }

        if (!TryNormalizeSchedule(session.Title, sessionDate, startTime, endTime, out var title, out var start, out var end, out validationError))
        {
            return false;
        }

        session.EventId = eventId;
        session.TenantId = tenantId;
        ApplyNormalizedSchedule(session, title, start, end);
        return true;
    }

    public static bool TryPrepareUpdateRequest(
        UpdateEventSessionRequest session,
        Guid eventId,
        Guid sessionId,
        DateTime? sessionDate,
        TimeSpan? startTime,
        TimeSpan? endTime,
        out string? validationError)
    {
        ArgumentNullException.ThrowIfNull(session);
        validationError = null;

        if (session.Id is null || session.Id == Guid.Empty || session.Id != sessionId)
        {
            validationError = "The session context is invalid. Return to the event draft and try again.";
            return false;
        }

        if (!TryNormalizeSchedule(session.Title, sessionDate, startTime, endTime, out var title, out var start, out var end, out validationError))
        {
            return false;
        }

        session.EventId = eventId;
        ApplyNormalizedSchedule(session, title, start, end);
        return true;
    }

    private static bool TryNormalizeSchedule(
        string? title,
        DateTime? sessionDate,
        TimeSpan? startTime,
        TimeSpan? endTime,
        out string normalizedTitle,
        out DateTime start,
        out DateTime end,
        out string? validationError)
    {
        normalizedTitle = string.Empty;
        start = default;
        end = default;
        validationError = null;

        if (string.IsNullOrWhiteSpace(title))
        {
            validationError = "Program item title is required.";
            return false;
        }

        if (!sessionDate.HasValue || !startTime.HasValue || !endTime.HasValue)
        {
            validationError = "Program item date, start time, and end time are required.";
            return false;
        }

        start = sessionDate.Value.Date + startTime.Value;
        end = sessionDate.Value.Date + endTime.Value;
        if (end <= start)
        {
            validationError = "End time must be after start time.";
            return false;
        }

        normalizedTitle = title.Trim();
        return true;
    }

    private static void ApplyNormalizedSchedule(ComposerCreateEventSessionRequest session, string title, DateTime start, DateTime end)
    {
        session.Title = title;
        session.MaxAudienceAttendees = session.MaxAudienceAttendees > 0 ? session.MaxAudienceAttendees : null;
        session.StartTime = DateTimeHelper.ConvertLocalToUtc(start);
        session.EndTime = DateTimeHelper.ConvertLocalToUtc(end);
    }

    private static void ApplyNormalizedSchedule(UpdateEventSessionRequest session, string title, DateTime start, DateTime end)
    {
        session.Title = title;
        session.MaxAudienceAttendees = session.MaxAudienceAttendees > 0 ? session.MaxAudienceAttendees : null;
        session.StartTime = DateTimeHelper.ConvertLocalToUtc(start);
        session.EndTime = DateTimeHelper.ConvertLocalToUtc(end);
    }

    private static bool TryParseDefaultTime(string? value, out TimeSpan time)
    {
        return TimeSpan.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            out time);
    }

    private static Guid? GetPrimarySessionGroupId(EventSessionDto session)
        => session.SessionGroups?
            .OrderByDescending(group => group.IsPrimary ?? false)
            .ThenBy(group => group.SortOrder ?? int.MaxValue)
            .Select(group => group.EventSessionGroupId)
            .FirstOrDefault(groupId => groupId.HasValue && groupId.Value != Guid.Empty);

    private static EventSessionIslamicAspectDto? MapIslamicAspect(EventSessionIslamicAspectDto? aspect) => aspect;
}

internal sealed record EventSessionCreateFormState(
    ICollection<EventSessionCreateLocationOptionDto> Locations,
    ICollection<EventSessionCreateGroupOptionDto> SessionGroups,
    DateTime? SessionDate,
    TimeSpan? StartTime,
    TimeSpan? EndTime);

internal sealed record EventSessionEditFormState(
    DateTime? SessionDate,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    Guid? PrimarySessionGroupId);
