// ABOUTME: Top-level agenda projection for an event, containing day groups with merged schedule entries.
// ABOUTME: Consumed by the Blazor CSS-grid agenda component and the API agenda endpoint.

namespace Explore.Application.DTOs.Agenda;

public class EventAgendaProjectionDto
{
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public string? Timezone { get; set; }

    public List<AgendaDayGroupDto> Days { get; set; } = [];
}
