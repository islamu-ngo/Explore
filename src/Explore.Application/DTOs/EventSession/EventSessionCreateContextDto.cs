// ABOUTME: Server-owned context for composing a new event program item.
// ABOUTME: Carries inherited event defaults and selector options for the dedicated session composer.

namespace Explore.Application.DTOs.EventSession;

public sealed record EventSessionCreateContextDto
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string? TimeZoneId { get; init; }
    public DateOnly? EventStartDate { get; init; }
    public DateOnly? EventEndDate { get; init; }
    public EventSessionCreateDefaultsDto Defaults { get; init; } = new();
    public List<EventSessionCreateLocationOptionDto> Locations { get; init; } = [];
    public List<EventSessionCreateRoomOptionDto> Rooms { get; init; } = [];
    public List<EventSessionCreateGroupOptionDto> SessionGroups { get; init; } = [];
    public List<string> Notices { get; init; } = [];
}

public sealed record EventSessionCreateDefaultsDto
{
    public DateOnly? SessionDate { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public int RegistrationModeId { get; init; }
}

public sealed record EventSessionCreateLocationOptionDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? TimeZoneId { get; init; }
}

public sealed record EventSessionCreateRoomOptionDto
{
    public Guid Id { get; init; }
    public Guid LocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? Capacity { get; init; }
    public int SortOrder { get; init; }
}

public sealed record EventSessionCreateGroupOptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? LocationId { get; init; }
    public string? LocationName { get; init; }
    public Guid? RoomId { get; init; }
    public string? RoomName { get; init; }
    public string? Color { get; init; }
    public int SortOrder { get; init; }
}
