// ABOUTME: Public session-agenda list DTO with purpose-scoped EventLocation disclosure.
// ABOUTME: Carries no physical Location identifier outside the constrained nested contract.

using Explore.Application.DTOs.Location;

namespace Explore.Application.DTOs.EventSessionAgendaItem;

public class EventSessionAgendaItemListDto
{
    public Guid Id { get; set; }

    // Event
    public Guid EventId { get; set; }

    // Tenant
    public Guid TenantId { get; set; }

    // Event Session
    public Guid EventSessionId { get; set; }
    public string? EventSessionTitle { get; set; }

    // Timing
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    // Details
    public required string Title { get; set; }
    public string? LocationFullName { get; set; }
    public EventLocationPublicDto? EventLocation { get; set; }
}
