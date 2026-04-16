// ABOUTME: Admin observability DTO exposing the current state of a custom-property projection per tenant.
// ABOUTME: Maps from CustomPropertyProjectionStatus entity for the projection admin endpoints.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyProjection;

public class ProjectionStatusDto
{
    public string ProjectionName { get; set; } = string.Empty;
    public int ProjectionVersion { get; set; }
    public Guid TenantId { get; set; }
    public CustomPropertyProjectionState State { get; set; }
    public DateTimeOffset? LastRebuildStartedAt { get; set; }
    public DateTimeOffset? LastRebuildCompletedAt { get; set; }
    public long RowsProcessed { get; set; }
    public long RowsFailed { get; set; }
    public string? LastCheckpoint { get; set; }
    public string? LastErrorMessage { get; set; }
}
