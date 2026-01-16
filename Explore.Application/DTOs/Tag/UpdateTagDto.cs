using System;

namespace Explore.Application.DTOs.Tag
{
    public class UpdateTagDto
    {
        public Guid Id { get; set; }
        public string MasterCode { get; set; }
        public string FullName { get; set; }
        public Guid TenantId { get; set; }
    }
}
