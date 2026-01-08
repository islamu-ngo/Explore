using System;

namespace Explore.Application.DTOs.Category
{
    public class CreateCategoryDto
    {
        public string MasterCode { get; set; }
        public string FullName { get; set; }
        public Guid? ParentId { get; set; }
        public Guid TenantId { get; set; }
    }
}
