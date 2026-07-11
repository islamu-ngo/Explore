// ABOUTME: Detail DTO for an auditable tenant-local role grant.
// ABOUTME: Used by GetTenantUserRoleGrantDetailsRequest for single-record responses.

namespace Explore.Application.DTOs.TenantUserRoleGrant;

public class TenantUserRoleGrantDto
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
    public Guid? GrantedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
