using System;

namespace Explore.Application.DTOs.EventCategories
{
    public class CreateEventCategoriesDto
    {
        public Guid EventId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid TenantId { get; set; }
    }
}
