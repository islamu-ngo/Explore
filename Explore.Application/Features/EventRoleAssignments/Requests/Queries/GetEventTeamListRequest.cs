// ABOUTME: CQRS query for listing event team members with role and lifecycle details.
// ABOUTME: Returns all assignments for an event; handler filters by effective status for non-admin views.

using Explore.Application.DTOs.EventRoleAssignment;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Queries;

public sealed class GetEventTeamListRequest : IRequest<List<EventTeamMemberDto>>
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public bool IncludeInactive { get; set; }
}
