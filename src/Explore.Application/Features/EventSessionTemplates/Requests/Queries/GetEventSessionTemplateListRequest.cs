// ABOUTME: Query request for paginated event session template lists scoped to a parent event template.
// ABOUTME: Session templates are owned children of event templates, so EventTemplateId is the primary filter.

using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplates.Requests.Queries;

public sealed record GetEventSessionTemplateListRequest(
    Guid EventTemplateId = default,
    int PageNumber = 1,
    int PageSize = PaginatedResult<EventSessionTemplateListDto>.DefaultPageSize)
    : IRequest<PaginatedResult<EventSessionTemplateListDto>>;
