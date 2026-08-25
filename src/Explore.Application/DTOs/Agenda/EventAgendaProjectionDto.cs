// ABOUTME: Top-level agenda projection for an event, containing day groups with merged schedule entries.
// ABOUTME: Consumed by the Blazor CSS-grid agenda component and the API agenda endpoint.

namespace Explore.Application.DTOs.Agenda;

public sealed record EventAgendaProjectionDto
{
    public Guid EventId { get; init; }
    public string? EventTitle { get; init; }
    public string? Timezone { get; init; }

    public List<AgendaDayGroupDto> Days { get; init; } = [];
}
