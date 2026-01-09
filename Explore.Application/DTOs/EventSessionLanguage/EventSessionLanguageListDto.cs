using System;

namespace Explore.Application.DTOs.EventSessionLanguage
{
    public class EventSessionLanguageListDto
    {
        public int Id { get; set; }
        public Guid EventSessionId { get; set; }
        public string? EventSessionTitle { get; set; }
        public int LanguageId { get; set; }
        public string? LanguageFullName { get; set; }
    }
}
