using System;

namespace Explore.Application.DTOs.EventSessionLanguage
{
    public class UpdateEventSessionLanguageDto
    {
        public int Id { get; set; }
        public Guid EventSessionId { get; set; }
        public int LanguageId { get; set; }
        public Guid TenantId { get; set; }
    }
}
