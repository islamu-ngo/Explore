using System;

namespace Explore.Application.DTOs.EventCategories;

public class UpdateEventCategoriesDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid TenantId { get; set; }
}
