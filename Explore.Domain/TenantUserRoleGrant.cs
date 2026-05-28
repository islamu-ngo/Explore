// ABOUTME: Auditable tenant-scoped role grant for a tenant-local user.
// ABOUTME: Replaces TenantMember as authority evidence rooted in TenantUser lifecycle state.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class TenantUserRoleGrant : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
    public Guid TenantUserId { get; set; }
    public required TenantUser TenantUser { get; set; }
    public int RoleId { get; set; }
    public required Role Role { get; set; }
    public int RoleScopeId { get; set; }
    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
