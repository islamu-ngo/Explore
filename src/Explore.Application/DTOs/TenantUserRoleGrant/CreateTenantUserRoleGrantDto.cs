// ABOUTME: DTO for granting a tenant-scoped role to an existing tenant-local user.
// ABOUTME: Clients provide TenantUserId and RoleId; the handler derives TenantId from context.

namespace Explore.Application.DTOs.TenantUserRoleGrant;

public sealed record CreateTenantUserRoleGrantDto
{
    public Guid TenantUserId { get; init; }
    public int RoleId { get; init; }
}
