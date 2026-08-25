// ABOUTME: Request payload for explicit event moderation lifecycle actions.
// ABOUTME: Carries structured audit reason metadata without accepting unsafe event content.

namespace Explore.Application.DTOs.Event;

public sealed record EventModerationRequestDto
{
    public string? ReasonCode { get; init; }
    public string? CorrelationId { get; init; }
}
