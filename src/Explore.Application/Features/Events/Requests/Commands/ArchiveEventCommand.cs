// ABOUTME: MediatR command for archiving an event via the explicit lifecycle transition.
// ABOUTME: Supplies event resource context for authorization before the archive handler runs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed record ArchiveEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; init; }
    public required ArchiveEventRequestDto Request { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(Guid.Empty, Id);
}
