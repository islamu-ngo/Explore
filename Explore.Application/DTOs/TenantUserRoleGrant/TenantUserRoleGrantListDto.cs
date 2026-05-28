// ABOUTME: List DTO for tenant-local role grant rows with user, tenant, and role labels.
// ABOUTME: Used by GetTenantUserRoleGrantListRequest for collection responses.

namespace Explore.Application.DTOs.TenantUserRoleGrant;

public class TenantUserRoleGrantListDto
{
    public Guid Id { get; set; }
    public Guid TenantUserId { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public required string UserFullName { get; set; }
    public Guid TenantId { get; set; }
    public required string TenantFullName { get; set; }
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
