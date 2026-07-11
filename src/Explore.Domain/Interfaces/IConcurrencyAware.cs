// ABOUTME: Interface for entities that support optimistic concurrency control.
// ABOUTME: Uses a Guid ConcurrencyStamp that is auto-updated by SaveChangesAsync in DbContext.

namespace Explore.Domain.Interfaces;

/// <summary>
/// Interface for entities that support optimistic concurrency control.
/// The ConcurrencyStamp is automatically set to a new Guid on every insert or update
/// by the DbContext SaveChangesAsync override, ensuring database-agnostic portability.
/// </summary>
public interface IConcurrencyAware
{
    /// <summary>
    /// Gets or sets the concurrency stamp used for optimistic concurrency control.
    /// Automatically managed by the persistence layer — do not set manually.
    /// </summary>
    Guid ConcurrencyStamp { get; set; }
}
