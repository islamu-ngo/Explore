// ABOUTME: Server-backed event program summary read model for progressive-disclosure shells.
// ABOUTME: Groups EventSession program items by section, local day, and readiness guidance.

namespace Explore.Application.DTOs.EventProgram;

public class EventProgramSummaryDto
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public List<EventProgramSectionDto> Sections { get; set; } = [];
    public List<EventProgramReadinessWarningDto> ReadinessWarnings { get; set; } = [];
}

public class EventProgramSectionDto
{
    public string SectionKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<EventProgramSessionGroupSectionDto> SessionGroups { get; set; } = [];
}

public class EventProgramSessionGroupSectionDto
{
    public Guid? SessionGroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Color { get; set; }
    public string? LocationName { get; set; }
    public string? RoomName { get; set; }
    public List<EventProgramDayGroupDto> Days { get; set; } = [];
}

public class EventProgramDayGroupDto
{
    public DateOnly LocalDate { get; set; }
    public string DisplayLabel { get; set; } = string.Empty;
    public List<EventProgramItemDto> Items { get; set; } = [];
}

public class EventProgramItemDto
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? EventSessionKindId { get; set; }
    public string? EventSessionKindName { get; set; }
    public string? EventSessionKindMasterCode { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public DateOnly LocalDate { get; set; }
    public TimeOnly LocalStartTime { get; set; }
    public TimeOnly LocalEndTime { get; set; }
    public int SortOrder { get; set; }
    public Guid? SessionGroupId { get; set; }
    public string? LocationName { get; set; }
    public string? RoomName { get; set; }
    public int? Capacity { get; set; }
    public string? RegistrationModeName { get; set; }
    public List<EventProgramReadinessWarningDto> ReadinessWarnings { get; set; } = [];
}

public class EventProgramReadinessWarningDto
{
    public string Path { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string Message { get; set; } = string.Empty;
}
