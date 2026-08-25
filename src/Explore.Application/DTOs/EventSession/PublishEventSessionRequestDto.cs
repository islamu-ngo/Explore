// ABOUTME: Request payload for publishing an event session through an explicit lifecycle transition.
// ABOUTME: Carries optimistic-concurrency state so stale publish requests fail before mutation.

namespace Explore.Application.DTOs.EventSession;

public sealed record PublishEventSessionRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; init; }
}
