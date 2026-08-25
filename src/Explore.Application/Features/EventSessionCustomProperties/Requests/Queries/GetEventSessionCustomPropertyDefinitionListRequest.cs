// ABOUTME: Query request for paginated session-local custom property definition lists.
// ABOUTME: Scoped to a specific event session so organizers see only their session's configuration.

using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Queries;

public sealed record GetEventSessionCustomPropertyDefinitionListRequest : IRequest<PaginatedResult<EventSessionCustomPropertyDefinitionListDto>>
{
    public Guid EventSessionId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PaginatedResult<EventSessionCustomPropertyDefinitionListDto>.DefaultPageSize;
}
