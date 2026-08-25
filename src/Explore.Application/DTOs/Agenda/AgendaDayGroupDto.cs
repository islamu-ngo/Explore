// ABOUTME: Groups schedule entries under a local date with optional EventDay metadata.
// ABOUTME: When an EventDay row exists, Label/Description/IsPublished come from it; otherwise the group is derived from sessions.

namespace Explore.Application.DTOs.Agenda;

public sealed record AgendaDayGroupDto
{
    public Guid? EventDayId { get; init; }
    public DateOnly LocalDate { get; init; }
    public string? Label { get; init; }
    public string? Description { get; init; }
    public bool IsPublished { get; init; }
    public int SortOrder { get; init; }
    public bool AllowsDayScopeRegistration { get; init; }

    public List<AgendaScheduleEntryDto> Entries { get; init; } = [];
}
