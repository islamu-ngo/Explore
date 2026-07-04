// ABOUTME: Query to retrieve current projection status rows for a tenant's event custom-property projections.
// ABOUTME: Returns status, rebuild timestamps, and error messages for operator observability.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.CustomPropertyProjections.View)]
public class GetEventCustomPropertyProjectionStatusQuery : IRequest<BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>>, ISecureRequest
{
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["authorizationScope"] = "tenant_projection_status",
            ["projectionName"] = IEventCustomPropertyProjectionUpdater.ProjectionName
        };
}
