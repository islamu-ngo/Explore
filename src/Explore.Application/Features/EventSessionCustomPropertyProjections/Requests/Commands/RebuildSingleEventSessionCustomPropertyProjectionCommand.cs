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

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => EventSessionId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["eventSessionId"] = EventSessionId.ToString("D"),
            ["authorizationScope"] = "single_event_session_projection_rebuild"
        };
}
