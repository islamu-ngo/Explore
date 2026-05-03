// ABOUTME: Query to retrieve current projection status rows for a tenant's event custom-property projections.
// ABOUTME: Returns status, rebuild timestamps, and error messages for operator observability.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.CustomPropertyProjections.View)]
public class GetEventCustomPropertyProjectionStatusQuery : IRequest<BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>>, ISecureRequest
{
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
