// ABOUTME: MediatR command for creating an unscheduled event-session draft.
// ABOUTME: Supplies parent event context for pre-create authorization and lifecycle readiness policy.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Create)]
public sealed class CreateDraftEventSessionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateDraftEventSessionRequestDto Request { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => Request.EventId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["eventId"] = Request.EventId.ToString(),
        ["authorizationPhase"] = AuthorizationPhases.PreCreate
    };
}
