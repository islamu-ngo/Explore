// ABOUTME: Server-backed event program summary read model for progressive-disclosure shells.
// ABOUTME: Groups EventSession program items by section, local day, and readiness guidance.

using Explore.Application.DTOs.Location;

namespace Explore.Application.DTOs.EventProgram;

public sealed record EventProgramSummaryDto
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string? TimeZoneId { get; init; }
    public List<EventProgramSectionDto> Sections { get; set; } = [];
    public List<EventProgramReadinessWarningDto> ReadinessWarnings { get; init; } = [];
}

public sealed record EventProgramSectionDto
{
    public string SectionKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public List<EventProgramSessionGroupSectionDto> SessionGroups { get; init; } = [];
}

public sealed record EventProgramSessionGroupSectionDto
{
    public Guid? SessionGroupId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public string? Color { get; init; }
    public string? LocationName { get; init; }
    public string? RoomName { get; init; }
    public EventLocationPublicDto? EventLocation { get; init; }
    public List<EventProgramDayGroupDto> Days { get; init; } = [];
}

public sealed record EventProgramDayGroupDto
{
    public DateOnly? LocalDate { get; init; }
    public string DisplayLabel { get; init; } = string.Empty;
    public List<EventProgramItemDto> Items { get; init; } = [];
}

public sealed record EventProgramItemDto
{
    public Guid SessionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? EventSessionKindId { get; init; }
    public string? EventSessionKindName { get; init; }
    public string? EventSessionKindMasterCode { get; init; }
    public DateTimeOffset? StartsAtUtc { get; init; }
    public DateTimeOffset? EndsAtUtc { get; init; }
    public DateOnly? LocalDate { get; init; }
    public TimeOnly? LocalStartTime { get; init; }
    public TimeOnly? LocalEndTime { get; init; }
    public int SortOrder { get; init; }
    public Guid? SessionGroupId { get; init; }
    public string? LocationName { get; init; }
    public string? RoomName { get; init; }
    public EventLocationPublicDto? EventLocation { get; init; }
    public int? Capacity { get; init; }
    public string? RegistrationModeName { get; init; }
    public List<EventProgramReadinessWarningDto> ReadinessWarnings { get; init; } = [];
}

public sealed record EventProgramReadinessWarningDto
{
    public string Path { get; init; } = string.Empty;
    public string Severity { get; init; } = "warning";
    public string Message { get; init; } = string.Empty;
}
