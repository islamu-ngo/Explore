// ABOUTME: Global concrete owner for system and service Actors that do not represent people or organizations.
// ABOUTME: Keeps machine identity separate from tenant participation and external unclassified subjects.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class ServicePrincipal : IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string DisplayName { get; set; }
    public Actor? Actor { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
