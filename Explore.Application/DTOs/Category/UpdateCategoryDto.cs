using System;

namespace Explore.Application.DTOs.Category
{
    public class UpdateCategoryDto
    {
        public Guid Id { get; set; }
        public string MasterCode { get; set; }
        public string FullName { get; set; }
        public Guid? ParentId { get; set; }
    }
}
