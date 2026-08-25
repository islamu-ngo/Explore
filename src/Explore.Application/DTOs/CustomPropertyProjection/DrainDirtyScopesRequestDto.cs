// ABOUTME: Request DTO for operator self-service dirty-scope drain without triggering a full rebuild.
// ABOUTME: Targets a specific projection by name for a given tenant.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public sealed record DrainDirtyScopesRequestDto
{
    public Guid TenantId { get; init; }
    public string ProjectionName { get; init; } = string.Empty;
}
