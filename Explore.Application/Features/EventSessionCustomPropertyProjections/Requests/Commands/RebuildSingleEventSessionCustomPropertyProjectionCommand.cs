// ABOUTME: Command to rebuild projection rows for a single event session.
// ABOUTME: Used by operators to repair individual session projection state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;

[AuthorizeResource("custom_property_projection", AuthorizationActions.Update)]
public class RebuildSingleEventSessionCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventSessionId { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
