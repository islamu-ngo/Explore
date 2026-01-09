using System;

namespace Explore.Application.DTOs.TagTypeTags
{
    public class CreateTagTypeTagsDto
    {
        public Guid TagId { get; set; }
        public int TagTypeId { get; set; }
        public Guid TenantId { get; set; }
    }
}
