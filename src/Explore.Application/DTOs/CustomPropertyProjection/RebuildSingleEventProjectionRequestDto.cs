// ABOUTME: Request DTO for rebuilding a single event's custom-property projection rows.
// ABOUTME: Used by operators to repair individual event projection state.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public sealed record RebuildSingleEventProjectionRequestDto
{
    public Guid EventId { get; init; }
}
