// ABOUTME: Request payload for explicit event moderation lifecycle actions.
// ABOUTME: Carries structured audit reason metadata without accepting unsafe event content.

namespace Explore.Application.DTOs.Event;

public sealed class EventModerationRequestDto
{
    public string? ReasonCode { get; set; }
    public string? CorrelationId { get; set; }
}
