// ABOUTME: Query request for paginated event template lists with optional event-type filtering.
// ABOUTME: Tenant scoping is handled by the handler via ITenantContext, not exposed in the request.

using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Queries;

public class GetEventTemplateListRequest : IRequest<PaginatedResult<EventTemplateListDto>>
{
    public int? EventTypeId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = PaginatedResult<EventTemplateListDto>.DefaultPageSize;
}
