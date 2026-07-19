// ABOUTME: Public session-agenda detail DTO with purpose-scoped EventLocation disclosure.
// ABOUTME: Retains legacy location fields only as a null compatibility seam during contract migration.

using Explore.Application.DTOs.Location;

namespace Explore.Application.DTOs.EventSessionAgendaItem;

public class EventSessionAgendaItemDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid EventSessionId { get; set; }
    public string? EventSessionTitle { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationFullName { get; set; }
    public EventLocationPublicDto? EventLocation { get; set; }
    public Guid TenantId { get; set; }
}
