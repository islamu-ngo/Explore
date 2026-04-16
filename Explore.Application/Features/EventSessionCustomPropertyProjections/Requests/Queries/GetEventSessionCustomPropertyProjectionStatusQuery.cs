// ABOUTME: Query to retrieve current projection status for event session custom-property projections.
// ABOUTME: Mirrors event projection status query for session scope.

using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;

public class GetEventSessionCustomPropertyProjectionStatusQuery : IRequest<BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>>
{
    public Guid TenantId { get; set; }
}
