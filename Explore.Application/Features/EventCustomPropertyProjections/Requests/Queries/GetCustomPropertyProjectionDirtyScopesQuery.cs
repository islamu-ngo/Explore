// ABOUTME: Query to retrieve pending dirty-scope backlog rows for operator inspection.
// ABOUTME: Paged to prevent excessive memory use on large backlogs.

using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;

public class GetCustomPropertyProjectionDirtyScopesQuery : IRequest<PaginatedResult<ProjectionDirtyScopeDto>>
{
    public Guid TenantId { get; set; }
    public string ProjectionName { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = PaginatedResult<ProjectionDirtyScopeDto>.DefaultPageSize;
}
