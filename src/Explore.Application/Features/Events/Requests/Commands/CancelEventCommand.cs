// ABOUTME: MediatR command for cancelling an event via the explicit lifecycle transition.
// ABOUTME: Supplies event resource context for authorization before the cancel handler runs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed class CancelEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }
    public required CancelEventRequestDto Request { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(Guid.Empty, Id);
}
