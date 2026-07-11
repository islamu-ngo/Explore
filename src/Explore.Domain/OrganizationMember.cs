using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class OrganizationMember : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    [ForeignKey("Organization")]
    public Guid OrganizationId { get; set; }
    public required Organization Organization { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    [ForeignKey("Role")]
    public int RoleId { get; set; }
    public required Role Role { get; set; }

    [ForeignKey("OrganizationPosition")]
    public int? OrganizationPositionId { get; set; }
    public OrganizationPosition? OrganizationPosition { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
