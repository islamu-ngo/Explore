// ABOUTME: Query request for paginated event session template lists scoped to a parent event template.
// ABOUTME: Session templates are owned children of event templates, so EventTemplateId is the primary filter.

using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplates.Requests.Queries;

public class GetEventSessionTemplateListRequest : IRequest<PaginatedResult<EventSessionTemplateListDto>>
{
    public Guid EventTemplateId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = PaginatedResult<EventSessionTemplateListDto>.DefaultPageSize;
}
