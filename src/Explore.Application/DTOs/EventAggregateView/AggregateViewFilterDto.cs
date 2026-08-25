// ABOUTME: Filter DTO for aggregate event-with-sessions read-model queries.
// ABOUTME: Keeps list-query filtering narrow to title, date, status, and visibility.

namespace Explore.Application.DTOs.EventAggregateView;

public sealed record AggregateViewFilterDto
{
    public string? Title { get; init; }
    public DateTimeOffset? StartAtFrom { get; init; }
    public DateTimeOffset? StartAtTo { get; init; }
    public string? Status { get; init; }
    public string? Visibility { get; init; }
}
