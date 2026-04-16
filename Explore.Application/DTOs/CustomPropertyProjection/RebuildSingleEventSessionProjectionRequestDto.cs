// ABOUTME: Request DTO for rebuilding a single event session's custom-property projection rows.
// ABOUTME: Used by operators to repair individual session projection state.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public class RebuildSingleEventSessionProjectionRequestDto
{
    public Guid EventSessionId { get; set; }
}
