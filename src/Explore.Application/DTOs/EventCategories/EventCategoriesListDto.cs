// ABOUTME: List DTO for event-category relationship rows.
// ABOUTME: Includes concurrency metadata for admin list update flows.

using System;

namespace Explore.Application.DTOs.EventCategories;

public class EventCategoriesListDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryFullName { get; set; }
    public string? CategoryMasterCode { get; set; } // For i18n with Tolgee
    public Guid TenantId { get; set; }
}
