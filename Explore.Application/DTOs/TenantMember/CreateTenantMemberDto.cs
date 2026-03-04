// ABOUTME: DTO for creating a new tenant member (user-role assignment within a tenant).
// ABOUTME: TenantId is set by the handler from context, not by the client.

namespace Explore.Application.DTOs.TenantMember;

public class CreateTenantMemberDto
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public int RoleId { get; set; }
}
