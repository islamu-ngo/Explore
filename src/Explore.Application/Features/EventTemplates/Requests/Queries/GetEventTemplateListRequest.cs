// ABOUTME: Query request for paginated event template lists with optional event-type filtering.
// ABOUTME: Tenant scoping is handled by the handler via ITenantContext, not exposed in the request.

using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Queries;

public sealed record GetEventTemplateListRequest : IRequest<PaginatedResult<EventTemplateListDto>>
{
    public int? EventTypeId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PaginatedResult<EventTemplateListDto>.DefaultPageSize;
}
