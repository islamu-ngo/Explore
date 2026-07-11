// ABOUTME: Read-only DTO for pending dirty-scope backlog rows surfaced to operators.
// ABOUTME: Enables inspection of which scopes are awaiting drain after rebuild contention.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyProjection;

public class ProjectionDirtyScopeDto
{
    public long Id { get; set; }
    public string ProjectionName { get; set; } = string.Empty;
    public int ProjectionVersion { get; set; }
    public Guid TenantId { get; set; }
    public CustomPropertyProjectionScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public Guid? DefinitionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DrainedAt { get; set; }
}
