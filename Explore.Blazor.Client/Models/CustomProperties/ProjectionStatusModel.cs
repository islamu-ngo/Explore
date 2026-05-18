// ABOUTME: Client model for the event / session custom-property projection health snapshot.
// ABOUTME: Populates the projection status section of the tenant governance page.

namespace Explore.Blazor.Client.Models.CustomProperties;

/// <summary>
/// Health snapshot of a custom-property projection for a tenant.
/// </summary>
public sealed class ProjectionStatusModel
{
    public string ProjectionName { get; set; } = string.Empty;
    public int ProjectionVersion { get; set; }
    public Guid? TenantId { get; set; }
    public int State { get; set; }
    public DateTimeOffset? LastRebuildStartedAt { get; set; }
    public DateTimeOffset? LastRebuildCompletedAt { get; set; }
    public long RowsProcessed { get; set; }
    public long RowsFailed { get; set; }
    public string? LastCheckpoint { get; set; }
    public string? LastErrorMessage { get; set; }
    public IReadOnlySet<string> LinkRelations { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasLink(string rel) => LinkRelations.Contains(rel);
}
