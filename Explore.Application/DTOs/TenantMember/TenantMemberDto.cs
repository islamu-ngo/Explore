// ABOUTME: Detail DTO for tenant member including user, tenant, role info and audit fields.
// ABOUTME: Used by GetTenantMemberDetailsRequest for single-record responses.

namespace Explore.Application.DTOs.TenantMember;

public class TenantMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public required string UserFullName { get; set; }
    public Guid TenantId { get; set; }
    public required string TenantFullName { get; set; }
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
