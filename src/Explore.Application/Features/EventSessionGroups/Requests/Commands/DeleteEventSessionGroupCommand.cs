// ABOUTME: MediatR command for soft-deleting an event session group without deleting sessions.
// ABOUTME: Handler uses repository soft-delete semantics to preserve program items.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSessionGroup, AuthorizationActions.Delete)]
public class DeleteEventSessionGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(TenantId, EventId);
}
