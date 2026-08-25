// ABOUTME: Request payload for server-selected global Actor moderation actions.
// ABOUTME: Exposes only the audit reason so clients cannot choose the moderation action.

namespace Explore.Application.DTOs.Actor;

public sealed record GlobalModerationRequestDto
{
    public string ReasonCode { get; init; } = string.Empty;
}
