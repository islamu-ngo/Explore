// ABOUTME: List read-model DTO for EventDay used in paginated collection responses.
// ABOUTME: Lightweight projection with key fields for day-level admin lists and agenda grouping.

namespace Explore.Application.DTOs.EventDay;

public sealed record EventDayListDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public DateOnly LocalDate { get; init; }
    public string? Label { get; init; }
    public bool IsPublished { get; init; }
    public int SortOrder { get; init; }
    public bool AllowsDayScopeRegistration { get; init; }
    public Guid ConcurrencyStamp { get; init; }
}
