// ABOUTME: MediatR command for assigning a session/program item to a program section or track.
// ABOUTME: Ensures assignments use EventSession rather than child Event hierarchy.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSessionGroup, AuthorizationActions.Update)]
public class AssignSessionToGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required AssignSessionToGroupRequestDto Assignment { get; set; }

    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => Assignment.EventId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(TenantId, Assignment.EventId);
}
