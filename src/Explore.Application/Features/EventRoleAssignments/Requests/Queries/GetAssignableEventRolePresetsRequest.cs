// ABOUTME: Query for event-role presets assignable by the current actor for one event.
// ABOUTME: Applies the deterministic same-event authority ceiling before returning UI choices.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventRoleAssignment;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTeam)]
public sealed record GetAssignableEventRolePresetsRequest : IRequest<List<EventRolePresetDto>>, ISecureRequest
{
    public Guid TenantId { get; init; }
    public Guid EventId { get; init; }
    public Guid AssignerUserId { get; init; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        EventId == Guid.Empty
        ? null
        : new EventScopedAuthorizationFacts(TenantId, EventId);
}
