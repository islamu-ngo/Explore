// ABOUTME: Request payload for archiving an event via the explicit lifecycle command.
// ABOUTME: Carries the optimistic-concurrency stamp required to safely transition state.

namespace Explore.Application.DTOs.Event;

public sealed record ArchiveEventRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; init; }
}
