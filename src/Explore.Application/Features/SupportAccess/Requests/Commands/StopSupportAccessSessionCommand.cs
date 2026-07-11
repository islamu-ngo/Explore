// ABOUTME: Authorized command for an actor to stop their own active support-access session.
// ABOUTME: Uses the persisted session ID rather than browser authority to end support mode.

using Explore.Application.Authorization;
using Explore.Application.DTOs.SupportAccess;
using MediatR;

namespace Explore.Application.Features.SupportAccess.Requests.Commands;

[AuthorizeResource(ResourceKinds.SupportAccessSession, AuthorizationActions.SupportAccessSessions.Stop)]
public sealed class StopSupportAccessSessionCommand : IRequest<SupportAccessSessionCommandResponseDto>, ISecureRequest
{
    public Guid SessionId { get; init; }
    public string? EndReasonText { get; init; }

    string? ISecureRequest.ResourceId => SessionId == Guid.Empty ? null : SessionId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["sessionId"] = SessionId.ToString("D")
    };
}
