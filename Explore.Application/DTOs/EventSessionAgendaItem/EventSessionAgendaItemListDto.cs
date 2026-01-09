using System;

namespace Explore.Application.DTOs.EventSessionAgendaItem
{
    public class EventSessionAgendaItemListDto
    {
        public Guid Id { get; set; }
        public Guid EventSessionId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string Title { get; set; }
        public string? LocationFullName { get; set; }
    }
}
