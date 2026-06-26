// ABOUTME: Request payload for scheduling an event session via the explicit lifecycle command.
// ABOUTME: Carries the optimistic-concurrency stamp and the new schedule window to apply.

namespace Explore.Application.DTOs.EventSession;

public sealed class ScheduleEventSessionRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}
