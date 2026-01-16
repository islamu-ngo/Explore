using System;

namespace Explore.Application.DTOs.TenantUser
{
    public class TenantUserDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserFullName { get; set; }
        public Guid TenantId { get; set; }
        public string TenantFullName { get; set; }
        public int UserRoleId { get; set; }
        public string UserRoleName { get; set; }
    }
}
