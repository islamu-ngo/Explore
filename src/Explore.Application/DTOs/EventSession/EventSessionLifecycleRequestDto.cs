// ABOUTME: Request payload for explicit event-session lifecycle transitions.
// ABOUTME: Carries optimistic-concurrency state so stale terminal-state requests fail before mutation.

namespace Explore.Application.DTOs.EventSession;

public sealed record EventSessionLifecycleRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; init; }
}
