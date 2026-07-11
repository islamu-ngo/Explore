// ABOUTME: Server-owned context for composing a new event program item.
// ABOUTME: Carries inherited event defaults and selector options for the dedicated session composer.

namespace Explore.Application.DTOs.EventSession;

public class EventSessionCreateContextDto
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string? TimeZoneId { get; set; }
    public DateOnly? EventStartDate { get; set; }
    public DateOnly? EventEndDate { get; set; }
    public EventSessionCreateDefaultsDto Defaults { get; set; } = new();
    public List<EventSessionCreateLocationOptionDto> Locations { get; set; } = [];
    public List<EventSessionCreateRoomOptionDto> Rooms { get; set; } = [];
    public List<EventSessionCreateGroupOptionDto> SessionGroups { get; set; } = [];
    public List<string> Notices { get; set; } = [];
}

public class EventSessionCreateDefaultsDto
{
    public DateOnly? SessionDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int RegistrationModeId { get; set; }
}

public class EventSessionCreateLocationOptionDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
}

public class EventSessionCreateRoomOptionDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
}

public class EventSessionCreateGroupOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}
