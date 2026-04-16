// ABOUTME: Command to rebuild projection rows for a single event.
// ABOUTME: Used by operators to repair individual event projection state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;

[AuthorizeResource("custom_property_projection", AuthorizationActions.Update)]
public class RebuildSingleEventCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
