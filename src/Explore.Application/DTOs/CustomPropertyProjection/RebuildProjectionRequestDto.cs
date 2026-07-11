// ABOUTME: Request DTO for triggering a tenant-wide custom-property projection rebuild.
// ABOUTME: Accepts optional batch size override; validated against quota limits.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public class RebuildProjectionRequestDto
{
    public Guid TenantId { get; set; }
    public int? BatchSize { get; set; }
}
