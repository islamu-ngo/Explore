// ABOUTME: MediatR command for assigning a schedule to an event session through an explicit transition.
// ABOUTME: Carries the target session id and schedule payload for authorization and handler validation.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public sealed class ScheduleEventSessionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }
    public required ScheduleEventSessionRequestDto Request { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
