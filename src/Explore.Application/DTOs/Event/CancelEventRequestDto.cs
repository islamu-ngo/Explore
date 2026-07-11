// ABOUTME: Request payload for cancelling an event via the explicit lifecycle command.
// ABOUTME: Carries the optimistic-concurrency stamp required to safely transition state.

namespace Explore.Application.DTOs.Event;

public sealed class CancelEventRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; set; }
}
