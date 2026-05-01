// ABOUTME: Query for event-role presets assignable by the current actor for one event.
// ABOUTME: Applies the deterministic same-event authority ceiling before returning UI choices.

using Explore.Application.DTOs.EventRoleAssignment;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Requests.Queries;

public sealed class GetAssignableEventRolePresetsRequest : IRequest<List<EventRolePresetDto>>
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid AssignerUserId { get; set; }
}
