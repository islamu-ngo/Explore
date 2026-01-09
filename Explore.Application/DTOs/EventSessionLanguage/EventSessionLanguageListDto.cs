using System;

namespace Explore.Application.DTOs.EventSessionLanguage
{
    public class EventSessionLanguageListDto
    {
        public int Id { get; set; }
        public Guid EventSessionId { get; set; }
        public string? EventSessionTitle { get; set; }
        public int LanguageId { get; set; }
        public string? LanguageMasterCode { get; set; } // For i18n with Tolgee
        public string? LanguageFullName { get; set; } // Fallback default
    }
}
