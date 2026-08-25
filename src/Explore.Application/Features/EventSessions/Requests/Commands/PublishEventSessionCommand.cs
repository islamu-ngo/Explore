// ABOUTME: MediatR command for publishing an event session through an explicit lifecycle transition.
// ABOUTME: Carries the target session id and concurrency payload for authorization and readiness validation.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public sealed record PublishEventSessionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; init; }
    public required PublishEventSessionRequestDto Request { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
