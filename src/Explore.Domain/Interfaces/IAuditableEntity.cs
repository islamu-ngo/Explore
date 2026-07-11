namespace Explore.Domain.Interfaces;

/// <summary>
/// Interface for entities that require audit tracking (created/updated timestamps and user).
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets when the entity was created (UTC).
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this entity.
    /// </summary>
    Guid? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets when the entity was last updated (UTC).
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated this entity.
    /// </summary>
    Guid? UpdatedBy { get; set; }
}
