// ABOUTME: Filter DTO for aggregate event-with-sessions read-model queries.
// ABOUTME: Keeps list-query filtering narrow to title, date, status, and visibility.

namespace Explore.Application.DTOs.EventAggregateView;

public sealed class AggregateViewFilterDto
{
    public string? Title { get; set; }
    public DateTimeOffset? StartAtFrom { get; set; }
    public DateTimeOffset? StartAtTo { get; set; }
    public string? Status { get; set; }
    public string? Visibility { get; set; }
}
