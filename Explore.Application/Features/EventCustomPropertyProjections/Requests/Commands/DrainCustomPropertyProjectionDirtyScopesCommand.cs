// ABOUTME: Command for operator self-service dirty-scope drain without triggering a full rebuild.
// ABOUTME: Idempotent — draining an already-empty backlog returns zero count.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.Update)]
public class DrainCustomPropertyProjectionDirtyScopesCommand : IRequest<BaseCommandResponse<DrainDirtyScopesResponseDto>>, ISecureRequest
{
    public required DrainDirtyScopesRequestDto RequestDto { get; set; }

    private Guid TenantId => RequestDto?.TenantId ?? Guid.Empty;
    private string ProjectionName => RequestDto?.ProjectionName ?? string.Empty;

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty
        ? null
        : string.IsNullOrWhiteSpace(ProjectionName)
            ? TenantId.ToString("D")
            : $"{TenantId:D}:{ProjectionName}";

    IDictionary<string, object>? ISecureRequest.ResourceAttributes
    {
        get
        {
            if (TenantId == Guid.Empty)
                return null;

            var attributes = new Dictionary<string, object>
            {
                ["tenantId"] = TenantId.ToString("D"),
                ["authorizationScope"] = "dirty_scope_drain"
            };

            if (!string.IsNullOrWhiteSpace(ProjectionName))
                attributes["projectionName"] = ProjectionName;

            return attributes;
        }
    }
}
