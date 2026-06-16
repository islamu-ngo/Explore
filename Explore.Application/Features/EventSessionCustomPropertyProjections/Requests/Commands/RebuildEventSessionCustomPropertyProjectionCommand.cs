// ABOUTME: Command to trigger a tenant-wide rebuild of event session custom-property projection rows.
// ABOUTME: Mirrors event projection rebuild with custom-property projection resource authorization metadata.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.Update)]
public class RebuildEventSessionCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<RebuildProjectionResponseDto>>, ISecureRequest
{
    public required RebuildProjectionRequestDto RequestDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
