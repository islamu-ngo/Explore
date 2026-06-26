// ABOUTME: Shared command contract for terminal event-session lifecycle transitions.
// ABOUTME: Lets handlers share concurrency, authorization, cache, and parent-event invariant behavior.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

public interface IEventSessionLifecycleTransitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    Guid Id { get; set; }
    EventSessionLifecycleRequestDto Request { get; set; }
}
