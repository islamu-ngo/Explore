// ABOUTME: Request payload for scheduling an event session via the explicit lifecycle command.
// ABOUTME: Carries the optimistic-concurrency stamp and the new schedule window to apply.

namespace Explore.Application.DTOs.EventSession;

public sealed record ScheduleEventSessionRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
}
