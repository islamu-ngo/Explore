// ABOUTME: Query to retrieve current projection status rows for a tenant's event custom-property projections.
// ABOUTME: Returns status, rebuild timestamps, and error messages for operator observability.

using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;

public class GetEventCustomPropertyProjectionStatusQuery : IRequest<BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>>
{
    public Guid TenantId { get; set; }
}
