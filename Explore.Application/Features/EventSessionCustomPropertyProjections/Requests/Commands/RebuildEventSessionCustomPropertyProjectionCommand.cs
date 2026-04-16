// ABOUTME: Command to trigger a tenant-wide rebuild of event session custom-property projection rows.
// ABOUTME: Mirrors event projection rebuild; authorized via property_governance_admin policy.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;

[AuthorizeResource("custom_property_projection", AuthorizationActions.Update)]
public class RebuildEventSessionCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<RebuildProjectionResponseDto>>, ISecureRequest
{
    public required RebuildProjectionRequestDto RequestDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
