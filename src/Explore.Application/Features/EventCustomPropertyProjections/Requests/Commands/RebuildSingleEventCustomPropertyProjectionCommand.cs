// ABOUTME: Command to rebuild projection rows for a single event.
// ABOUTME: Used by operators to repair individual event projection state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.Update)]
public class RebuildSingleEventCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => EventId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["eventId"] = EventId.ToString("D"),
            ["authorizationScope"] = "single_event_projection_rebuild"
        };
}
