using System;

namespace Explore.Application.DTOs.TenantUser
{
    public class CreateTenantUserDto
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public int UserRoleId { get; set; }
    }
}
