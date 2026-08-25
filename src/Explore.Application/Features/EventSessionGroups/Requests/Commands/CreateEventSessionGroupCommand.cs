// ABOUTME: MediatR command for creating an event session group such as a track or stage.
// ABOUTME: Secured through the canonical event_session_group resource kind.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSessionGroup, AuthorizationActions.Create)]
public sealed record CreateEventSessionGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventSessionGroupRequestDto EventSessionGroup { get; init; }

    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => EventSessionGroup.EventId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(TenantId, EventSessionGroup.EventId);
}
