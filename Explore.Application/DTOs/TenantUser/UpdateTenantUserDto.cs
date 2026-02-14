using System;

namespace Explore.Application.DTOs.TenantUser;

public class UpdateTenantUserDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public int RoleId { get; set; }
}
