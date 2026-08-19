// ABOUTME: CQRS query for listing event team members with role and lifecycle details.
// ABOUTME: Returns all assignments for an event; handler filters by effective status for non-admin views.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventRoleAssignment;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTeam)]
public sealed class GetEventTeamListRequest : IRequest<List<EventTeamMemberDto>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public bool IncludeInactive { get; set; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        EventId == Guid.Empty
        ? null
        : new EventScopedAuthorizationFacts(TenantId, EventId);
}
