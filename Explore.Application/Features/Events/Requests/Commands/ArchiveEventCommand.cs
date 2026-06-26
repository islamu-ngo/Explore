// ABOUTME: MediatR command for archiving an event via the explicit lifecycle transition.
// ABOUTME: Supplies event resource context for authorization before the archive handler runs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed class ArchiveEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }
    public required ArchiveEventRequestDto Request { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["eventId"] = Id.ToString()
    };
}
