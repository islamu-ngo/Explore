using System;

namespace Explore.Application.DTOs.TagTypeTags
{
    public class UpdateTagTypeTagsDto
    {
        public Guid Id { get; set; }
        public Guid TagId { get; set; }
        public int TagTypeId { get; set; }
    }
}
