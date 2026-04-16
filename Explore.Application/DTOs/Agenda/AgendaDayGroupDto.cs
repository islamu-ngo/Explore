// ABOUTME: Groups schedule entries under a local date with optional EventDay metadata.
// ABOUTME: When an EventDay row exists, Label/Description/IsPublished come from it; otherwise the group is derived from sessions.

namespace Explore.Application.DTOs.Agenda;

public class AgendaDayGroupDto
{
    public Guid? EventDayId { get; set; }
    public DateOnly LocalDate { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public bool AllowsDayScopeRegistration { get; set; }

    public List<AgendaScheduleEntryDto> Entries { get; set; } = [];
}
