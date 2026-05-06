// ABOUTME: Handler assembling the server-backed event program summary from event sessions and groups.
// ABOUTME: Applies local-day grouping and readiness guidance inside Application layer boundaries.

using System.Globalization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventProgram;
using Explore.Application.Features.EventPrograms.Models;
using Explore.Application.Features.EventPrograms.Requests.Queries;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventPrograms.Handlers.Queries;

public class GetEventProgramSummaryRequestHandler : IRequestHandler<GetEventProgramSummaryRequest, EventProgramSummaryDto?>
{
    private const string UnassignedSectionKey = "unassigned";
    private const int UnassignedSortOrder = int.MaxValue;

    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;

    public GetEventProgramSummaryRequestHandler(
        IEventRepository eventRepository,
        IEventSessionRepository eventSessionRepository,
        IEventSessionGroupRepository eventSessionGroupRepository)
    {
        _eventRepository = eventRepository;
        _eventSessionRepository = eventSessionRepository;
        _eventSessionGroupRepository = eventSessionGroupRepository;
    }

    public async Task<EventProgramSummaryDto?> Handle(GetEventProgramSummaryRequest request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetEventWithDetails(request.EventId);
        if (eventEntity is null)
            return null;

        var sessions = await _eventSessionRepository.GetSessionsByEvent(request.EventId);
        var groups = await _eventSessionGroupRepository.GetByEventAsync(request.EventId, cancellationToken);
        var groupLookup = groups.ToDictionary(group => group.Id);
        var timezoneId = eventEntity.EventTimeZoneId ?? eventEntity.Timezone;
        var timezone = ResolveTimeZone(timezoneId);

        var summary = new EventProgramSummaryDto
        {
            EventId = eventEntity.Id,
            EventTitle = eventEntity.Title,
            TimeZoneId = timezoneId
        };

        AddGlobalWarnings(summary, eventEntity, timezoneId, groups, sessions);

        var programGroups = BuildProgramGroups(sessions, groupLookup, timezone, eventEntity, summary);
        summary.Sections = programGroups
            .OrderBy(group => group.SortOrder)
            .ThenBy(group => group.Title)
            .Select(group => new EventProgramSectionDto
            {
                SectionKey = group.SectionKey,
                Title = group.Title,
                SortOrder = group.SortOrder,
                SessionGroups = [BuildSessionGroupSection(group)]
            })
            .ToList();

        return summary;
    }

    private static List<ProgramGroupAccumulator> BuildProgramGroups(
        IEnumerable<EventSession> sessions,
        IReadOnlyDictionary<Guid, EventSessionGroup> groupLookup,
        TimeZoneInfo timezone,
        Event eventEntity,
        EventProgramSummaryDto summary)
    {
        var accumulators = new Dictionary<string, ProgramGroupAccumulator>(StringComparer.Ordinal);

        foreach (var session in sessions)
        {
            var primaryAssignment = session.SessionGroups
                .Where(assignment => !assignment.IsDeleted && groupLookup.ContainsKey(assignment.EventSessionGroupId))
                .OrderByDescending(assignment => assignment.IsPrimary)
                .ThenBy(assignment => assignment.SortOrder)
                .FirstOrDefault();
            var group = primaryAssignment is null ? null : groupLookup[primaryAssignment.EventSessionGroupId];
            var sectionKey = group?.Id.ToString() ?? UnassignedSectionKey;

            if (!accumulators.TryGetValue(sectionKey, out var accumulator))
            {
                accumulator = group is null
                    ? ProgramGroupAccumulator.Unassigned(UnassignedSectionKey, UnassignedSortOrder)
                    : ProgramGroupAccumulator.FromGroup(group);
                accumulators[sectionKey] = accumulator;
            }

            var item = BuildProgramItem(session, primaryAssignment, group, timezone, eventEntity);
            accumulator.Items.Add(item);

            if (primaryAssignment is null)
            {
                AddWarning(summary, $"sessions/{session.Id}", "Assign this program item to a section, track, devroom, or stage before publishing.");
                item.ReadinessWarnings.Add(CreateWarning($"sessions/{session.Id}", "Program section is not assigned."));
            }

            AddSessionWarnings(summary, item, session, eventEntity);
        }

        return accumulators.Values.ToList();
    }

    private static EventProgramItemDto BuildProgramItem(
        EventSession session,
        EventSessionGroupSession? assignment,
        EventSessionGroup? group,
        TimeZoneInfo timezone,
        Event eventEntity)
    {
        var localStart = session.LocalStartDate == default
            ? TimeZoneInfo.ConvertTime(session.StartTime, timezone)
            : new DateTimeOffset(
                session.LocalStartDate.ToDateTime(session.LocalStartTime),
                TimeZoneInfo.ConvertTime(session.StartTime, timezone).Offset);
        var localEnd = session.LocalEndDate == default
            ? TimeZoneInfo.ConvertTime(session.EndTime, timezone)
            : new DateTimeOffset(
                session.LocalEndDate.ToDateTime(session.LocalEndTime),
                TimeZoneInfo.ConvertTime(session.EndTime, timezone).Offset);

        return new EventProgramItemDto
        {
            SessionId = session.Id,
            Title = string.IsNullOrWhiteSpace(session.Title) ? "Untitled program item" : session.Title.Trim(),
            StartsAtUtc = session.StartTime,
            EndsAtUtc = session.EndTime,
            LocalDate = DateOnly.FromDateTime(localStart.DateTime),
            LocalStartTime = TimeOnly.FromDateTime(localStart.DateTime),
            LocalEndTime = TimeOnly.FromDateTime(localEnd.DateTime),
            SortOrder = assignment?.SortOrder ?? session.SortOrder,
            SessionGroupId = group?.Id,
            LocationName = session.Location?.FullName ?? group?.Location?.FullName,
            RoomName = session.Room?.Name ?? group?.Room?.Name,
            Capacity = session.MaxAudienceAttendees,
            RegistrationModeName = session.RegistrationMode?.FullName
        };
    }

    private static EventProgramSessionGroupSectionDto BuildSessionGroupSection(ProgramGroupAccumulator accumulator)
    {
        return new EventProgramSessionGroupSectionDto
        {
            SessionGroupId = accumulator.SessionGroupId,
            Title = accumulator.Title,
            SortOrder = accumulator.SortOrder,
            Color = accumulator.Color,
            LocationName = accumulator.LocationName,
            RoomName = accumulator.RoomName,
            Days = accumulator.Items
                .GroupBy(item => item.LocalDate)
                .OrderBy(group => group.Key)
                .Select(group => new EventProgramDayGroupDto
                {
                    LocalDate = group.Key,
                    DisplayLabel = group.Key.ToString("ddd d MMM", CultureInfo.InvariantCulture),
                    Items = group
                        .OrderBy(item => item.SortOrder)
                        .ThenBy(item => item.LocalStartTime)
                        .ThenBy(item => item.Title)
                        .ToList()
                })
                .ToList()
        };
    }

    private static void AddGlobalWarnings(
        EventProgramSummaryDto summary,
        Event eventEntity,
        string? timezoneId,
        IReadOnlyCollection<EventSessionGroup> groups,
        IReadOnlyCollection<EventSession> sessions)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            AddWarning(summary, "event/timezone", "No event timezone is configured. Program summary uses UTC until a timezone is set.");
        }

        if (!eventEntity.FirstSessionDate.HasValue || !eventEntity.LastSessionDate.HasValue)
        {
            AddWarning(summary, "event/date-window", "No event date window is configured. Confirm program item dates before publishing.");
        }

        if (groups.Count == 0)
        {
            AddWarning(summary, "event/program-sections", "No program sections exist yet. Add a section, track, devroom, or stage to organize sessions.");
        }

        if (sessions.Count == 0)
        {
            AddWarning(summary, "event/program-items", "No program items exist yet. Add talks, workshops, panels, classes, or activities as sessions.");
        }
    }

    private static void AddSessionWarnings(EventProgramSummaryDto summary, EventProgramItemDto item, EventSession session, Event eventEntity)
    {
        if (string.IsNullOrWhiteSpace(session.Title))
        {
            AddWarning(summary, $"sessions/{session.Id}/title", "A program item is missing a title.");
            item.ReadinessWarnings.Add(CreateWarning($"sessions/{session.Id}/title", "Title is missing."));
        }

        if (session.LocationId is null && session.RoomId is null)
        {
            AddWarning(summary, $"sessions/{session.Id}/location", $"{item.Title} has no location or room assigned.");
            item.ReadinessWarnings.Add(CreateWarning($"sessions/{session.Id}/location", "Location or room is missing."));
        }

        if (session.MaxAudienceAttendees is null)
        {
            AddWarning(summary, $"sessions/{session.Id}/capacity", $"{item.Title} has no capacity configured.");
        }

        if (session.RegistrationModeId is null)
        {
            AddWarning(summary, $"sessions/{session.Id}/registration", $"{item.Title} has no registration mode configured.");
        }

        if (eventEntity.FirstSessionDate.HasValue && item.LocalDate < eventEntity.FirstSessionDate.Value ||
            eventEntity.LastSessionDate.HasValue && item.LocalDate > eventEntity.LastSessionDate.Value)
        {
            AddWarning(summary, $"sessions/{session.Id}/date", $"{item.Title} is outside the event date window.");
            item.ReadinessWarnings.Add(CreateWarning($"sessions/{session.Id}/date", "Outside event date window."));
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string? timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static void AddWarning(EventProgramSummaryDto summary, string path, string message)
    {
        summary.ReadinessWarnings.Add(CreateWarning(path, message));
    }

    private static EventProgramReadinessWarningDto CreateWarning(string path, string message)
    {
        return new EventProgramReadinessWarningDto
        {
            Path = path,
            Message = message
        };
    }

}
