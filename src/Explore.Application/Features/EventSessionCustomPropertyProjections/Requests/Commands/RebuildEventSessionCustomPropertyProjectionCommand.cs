// ABOUTME: Command to trigger a tenant-wide rebuild of event session custom-property projection rows.
// ABOUTME: Mirrors event projection rebuild with custom-property projection resource authorization metadata.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.Update)]
public class RebuildEventSessionCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<RebuildProjectionResponseDto>>, ISecureRequest
{
    public required RebuildProjectionRequestDto RequestDto { get; set; }

    private Guid TenantId => RequestDto?.TenantId ?? Guid.Empty;

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["authorizationScope"] = "tenant_projection_rebuild",
            ["projectionName"] = IEventSessionCustomPropertyProjectionUpdater.ProjectionName
        };
}
