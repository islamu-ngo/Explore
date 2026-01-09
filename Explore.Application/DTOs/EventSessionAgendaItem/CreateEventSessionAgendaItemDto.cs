using System;

namespace Explore.Application.DTOs.EventSessionAgendaItem
{
    public class CreateEventSessionAgendaItemDto
    {
        public Guid EventSessionId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public Guid? LocationId { get; set; }
        public Guid TenantId { get; set; }
    }
}
