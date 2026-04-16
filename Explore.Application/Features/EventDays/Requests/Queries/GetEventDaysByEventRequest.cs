// ABOUTME: MediatR query for retrieving all EventDays belonging to a specific event.
// ABOUTME: Returns a list (not paginated) since days per event are typically small (< 30).

using Explore.Application.DTOs.EventDay;
using MediatR;

namespace Explore.Application.Features.EventDays.Requests.Queries;

public class GetEventDaysByEventRequest : IRequest<List<EventDayListDto>>
{
    public Guid EventId { get; set; }
}
