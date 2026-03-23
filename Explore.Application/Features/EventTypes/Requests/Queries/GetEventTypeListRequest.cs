// ABOUTME: MediatR query request for fetching all event types.
// ABOUTME: Returns IEnumerable<EventTypeDto>.
using Explore.Application.DTOs.EventType;
using MediatR;

namespace Explore.Application.Features.EventTypes.Requests.Queries;

public class GetEventTypeListRequest : IRequest<List<EventTypeListDto>>
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
