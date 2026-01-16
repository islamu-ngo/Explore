using System;

namespace Explore.Application.DTOs.Tenant
{
    public class UpdateTenantDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
    }
}
