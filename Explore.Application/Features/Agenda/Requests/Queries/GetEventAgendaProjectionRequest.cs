// ABOUTME: MediatR query for retrieving the full agenda projection for an event grouped by local day.
// ABOUTME: Merges EventSession and EventAgendaItem into unified schedule entries per day.

using Explore.Application.DTOs.Agenda;
using MediatR;

namespace Explore.Application.Features.Agenda.Requests.Queries;

public class GetEventAgendaProjectionRequest : IRequest<EventAgendaProjectionDto?>
{
    public Guid EventId { get; set; }
}
