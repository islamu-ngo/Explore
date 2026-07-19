// ABOUTME: Unified schedule entry DTO merging EventSession and EventAgendaItem into a single timeline item.
// ABOUTME: Discriminated by EntryType so the UI renders sessions and agenda items differently in the same grid.

using Explore.Application.DTOs.Location;

namespace Explore.Application.DTOs.Agenda;

public class AgendaScheduleEntryDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Discriminator: "Session" or "AgendaItem".
    /// </summary>
    public required string EntryType { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public DateOnly LocalStartDate { get; set; }
    public TimeOnly LocalStartTime { get; set; }
    public TimeOnly LocalEndTime { get; set; }
    public int LocalStartMinuteOfDay { get; set; }
    public int LocalEndMinuteOfDay { get; set; }

    public Guid? RoomId { get; set; }
    public Guid? LocationId { get; set; }
    public EventLocationPublicDto? EventLocation { get; set; }

    // Session-specific fields (null for agenda items)
    public int? MaxAudienceAttendees { get; set; }
    public int? CurrentAudienceAttendees { get; set; }
    public int? RegistrationModeId { get; set; }
    public string? RegistrationModeFullName { get; set; }
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // AgendaItem-specific fields (null for sessions)
    public int? KindId { get; set; }
    public string? KindFullName { get; set; }

    public int SortOrder { get; set; }
}
