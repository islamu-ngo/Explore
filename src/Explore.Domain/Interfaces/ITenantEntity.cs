namespace Explore.Domain.Interfaces;

/// <summary>
/// Marker interface for entities that are tenant-scoped.
/// Used by Global Query Filters to automatically filter by TenantId.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// Gets or sets the tenant identifier for this entity.
    /// </summary>
    Guid TenantId { get; set; }
}
