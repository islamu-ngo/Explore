// ABOUTME: Maps users to tenant-level membership with roles from the unified Role table.
// ABOUTME: Replaces TenantAdministrator. Mirrors OrganizationMember pattern for consistency.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class TenantMember : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [ForeignKey(nameof(Role))]
    public int RoleId { get; set; }
    public required Role Role { get; set; }

    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }

    // Audit fields (IAuditableEntity)
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
