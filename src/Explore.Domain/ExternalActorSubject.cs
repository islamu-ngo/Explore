// ABOUTME: Global concrete owner for an Actor whose represented subject is not yet classified.
// ABOUTME: Preserves one stable subject identity across tenant observations until verified promotion.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class ExternalActorSubject : IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public Actor? Actor { get; set; }
    public DateTime FirstObservedAt { get; set; }
    public DateTime LastObservedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
