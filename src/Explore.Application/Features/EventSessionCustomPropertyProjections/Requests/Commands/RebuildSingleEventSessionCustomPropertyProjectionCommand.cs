// ABOUTME: Command to rebuild projection rows for a single event session.
// ABOUTME: Used by operators to repair individual session projection state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.Update)]
public class RebuildSingleEventSessionCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventSessionId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId == Guid.Empty ? null : EventSessionId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        EventSessionId == Guid.Empty
        ? null
        : new CustomPropertyProjectionAuthorizationFacts(Guid.Empty, null, EventSessionId);
}
