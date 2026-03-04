// ABOUTME: DTO for updating an existing tenant member's role assignment.
// ABOUTME: TenantId is set by the handler from context, not by the client.

namespace Explore.Application.DTOs.TenantMember;

public class UpdateTenantMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public int RoleId { get; set; }
}
