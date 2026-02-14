using System;

namespace Explore.Application.DTOs.TenantUser;

public class TenantUserListDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public required string UserFullName { get; set; }
    public Guid TenantId { get; set; }
    public required string TenantFullName { get; set; }
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
}
