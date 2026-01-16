using System;

namespace Explore.Application.DTOs.Tenant
{
    public class CreateTenantDto
    {
        public string FullName { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
    }
}
