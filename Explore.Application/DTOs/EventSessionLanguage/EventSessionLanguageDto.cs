// ABOUTME: Detail DTO for an event-session language assignment.
// ABOUTME: Exposes concurrency metadata for route-ID PATCH If-Match preconditions.

using System;

namespace Explore.Application.DTOs.EventSessionLanguage;

public class EventSessionLanguageDto
{
    public int Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid EventSessionId { get; set; }
    public string? EventSessionTitle { get; set; }
    public int LanguageId { get; set; }
    public string? LanguageMasterCode { get; set; } // For i18n with Tolgee
    public string? LanguageFullName { get; set; } // Fallback default
    public Guid TenantId { get; set; }
}
