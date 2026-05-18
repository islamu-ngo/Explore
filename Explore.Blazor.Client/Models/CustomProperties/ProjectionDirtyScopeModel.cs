// ABOUTME: Client model for a pending projection dirty-scope awaiting drain.
// ABOUTME: Displayed in the projection status section as drainable work.

namespace Explore.Blazor.Client.Models.CustomProperties;

/// <summary>
/// A pending projection repair scope recorded by the outbox after a failure or partial update.
/// </summary>
public sealed class ProjectionDirtyScopeModel
{
    public long Id { get; set; }
    public string ProjectionName { get; set; } = string.Empty;
    public int ProjectionVersion { get; set; }
    public Guid? TenantId { get; set; }
    public int ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
    public Guid? DefinitionId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? DrainedAt { get; set; }
    public IReadOnlySet<string> LinkRelations { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasLink(string rel) => LinkRelations.Contains(rel);
}
