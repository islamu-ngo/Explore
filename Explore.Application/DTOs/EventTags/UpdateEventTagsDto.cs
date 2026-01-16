using System;

namespace Explore.Application.DTOs.EventTags
{
    public class UpdateEventTagsDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid TagId { get; set; }
        public Guid TenantId { get; set; }
    }
}
