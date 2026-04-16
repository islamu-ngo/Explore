// ABOUTME: Request DTO for operator self-service dirty-scope drain without triggering a full rebuild.
// ABOUTME: Targets a specific projection by name for a given tenant.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public class DrainDirtyScopesRequestDto
{
    public Guid TenantId { get; set; }
    public string ProjectionName { get; set; } = string.Empty;
}
