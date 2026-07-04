// ABOUTME: Protected query for API composition that resolves EventSession authorization context.
// ABOUTME: Uses EventSession resource authorization so management routes work for draft sessions.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public sealed class GetEventSessionAuthorizationContextRequest : IRequest<EventSessionAuthorizationContextDto?>, ISecureRequest
{
    public Guid EventSessionId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId.ToString();
}
