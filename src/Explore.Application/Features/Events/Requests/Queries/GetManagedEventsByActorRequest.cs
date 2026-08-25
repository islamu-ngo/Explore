// ABOUTME: MediatR query request for authenticated actor-profile management event lists.
// ABOUTME: Returns only events the current principal can manage/view through event view-management authorization.

using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record GetManagedEventsByActorRequest : IRequest<PaginatedResult<EventListDto>>
{
    public Guid ActorId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
