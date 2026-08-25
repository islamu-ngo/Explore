// ABOUTME: MediatR command for tenant/instance administrators to update Group approval state.
// ABOUTME: Keeps approval management separate from ordinary group metadata editing.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Group;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Groups.Requests.Commands;

[AuthorizeResource(ResourceKinds.Group, AuthorizationActions.Update)]
public sealed record UpdateGroupApprovalStatusCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; init; }
    public required UpdateGroupApprovalStatusDto GroupApprovalStatusDto { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
