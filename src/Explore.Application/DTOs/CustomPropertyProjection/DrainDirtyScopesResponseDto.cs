// ABOUTME: Response DTO for a dirty-scope drain operation with the number of scopes processed.
// ABOUTME: Returns the count so operators can gauge backlog health.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public sealed record DrainDirtyScopesResponseDto
{
    public int DrainedCount { get; init; }
    public DateTimeOffset DrainedAt { get; init; }
}
