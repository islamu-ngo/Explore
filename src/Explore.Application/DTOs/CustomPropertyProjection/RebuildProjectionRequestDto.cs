// ABOUTME: Request DTO for triggering a tenant-wide custom-property projection rebuild.
// ABOUTME: Accepts optional batch size override; validated against quota limits.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public sealed record RebuildProjectionRequestDto
{
    public Guid TenantId { get; init; }
    public int? BatchSize { get; init; }
}
