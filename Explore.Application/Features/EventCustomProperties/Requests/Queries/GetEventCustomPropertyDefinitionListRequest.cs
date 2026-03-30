// ABOUTME: Query request for paginated event-local custom property definition lists.
// ABOUTME: Scoped to a specific event so organizers see only their event's configuration.

using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Queries;

public class GetEventCustomPropertyDefinitionListRequest : IRequest<PaginatedResult<EventCustomPropertyDefinitionListDto>>
{
    public Guid EventId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = PaginatedResult<EventCustomPropertyDefinitionListDto>.DefaultPageSize;
}
