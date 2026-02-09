using System;

namespace Explore.Application.DTOs.EventTags;

public class EventTagsListDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid TagId { get; set; }
    public string? TagFullName { get; set; }
    public string? TagMasterCode { get; set; } // For i18n with Tolgee
    public Guid TenantId { get; set; }
}
