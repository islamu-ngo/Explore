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

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
