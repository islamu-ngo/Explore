// ABOUTME: MediatR query request for authenticated actor-profile management event lists.
// ABOUTME: Returns only events the current principal can manage/view through event view-management authorization.

using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public class GetManagedEventsByActorRequest : IRequest<PaginatedResult<EventListDto>>
{
    public Guid ActorId { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
