using System;

namespace Explore.Application.DTOs.EventCategories;

public class EventCategoriesDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryFullName { get; set; }
    public string? CategoryMasterCode { get; set; } // For i18n with Tolgee
    public Guid TenantId { get; set; }
}
