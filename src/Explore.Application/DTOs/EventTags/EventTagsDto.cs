// ABOUTME: Detail DTO for an event-tag relationship row.
// ABOUTME: Exposes concurrency metadata so clients can submit strong update preconditions.

using System;

namespace Explore.Application.DTOs.EventTags;

public class EventTagsDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid TagId { get; set; }
    public string? TagFullName { get; set; }
    public string? TagMasterCode { get; set; } // For i18n with Tolgee
    public Guid TenantId { get; set; }
}
