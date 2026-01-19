namespace Explore.Domain.Interfaces;

/// <summary>
/// Marker interface for entities that support soft deletion.
/// Used by Global Query Filters to automatically exclude deleted entities.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets whether this entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets when the entity was soft-deleted (UTC).
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who soft-deleted this entity.
    /// </summary>
    Guid? DeletedBy { get; set; }
}
