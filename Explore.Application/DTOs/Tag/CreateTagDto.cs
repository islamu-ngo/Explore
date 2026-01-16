using System;

namespace Explore.Application.DTOs.Tag
{
    public class CreateTagDto
    {
        public string MasterCode { get; set; }
        public string FullName { get; set; }
        public Guid TenantId { get; set; }
    }
}
