using System;

namespace Explore.Application.DTOs.EventSessionAgendaItem;

public class EventSessionAgendaItemDto
{
    public Guid Id { get; set; }
    public Guid EventSessionId { get; set; }
    public string? EventSessionTitle { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationFullName { get; set; }
    public Guid TenantId { get; set; }
}
