// ABOUTME: List DTO for event-session language assignments.
// ABOUTME: Includes concurrency metadata so list-driven editors can issue safe PATCH requests.

using System;

namespace Explore.Application.DTOs.EventSessionLanguage;

public class EventSessionLanguageListDto
{
    public int Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid EventSessionId { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public string? EventSessionTitle { get; set; }
    public int LanguageId { get; set; }
    public string? LanguageMasterCode { get; set; } // For i18n with Tolgee
    public string? LanguageFullName { get; set; } // Fallback default
}
