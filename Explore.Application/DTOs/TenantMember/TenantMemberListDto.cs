// ABOUTME: List DTO for tenant member with essential user, tenant, and role info.
// ABOUTME: Used by GetTenantMemberListRequest for collection responses.

namespace Explore.Application.DTOs.TenantMember;

public class TenantMemberListDto
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
}
