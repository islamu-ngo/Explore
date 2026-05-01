// ABOUTME: Handler for revoking event-role assignments without deleting audit evidence.
// ABOUTME: Protects the last effective direct EventOwner assignment.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventRoleAssignments.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Handlers.Commands;

public sealed class RevokeEventRoleAssignmentCommandHandler
    : EventRoleAssignmentCommandHandlerBase,
      IRequestHandler<RevokeEventRoleAssignmentCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRoleAssignmentRepository _assignmentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventRoleAuthorityCeilingService _authorityCeilingService;
    private readonly BusinessMetrics _metrics;

    public RevokeEventRoleAssignmentCommandHandler(
        IEventRoleAssignmentRepository assignmentRepository,
        IEventRepository eventRepository,
        IEventRoleAuthorityCeilingService authorityCeilingService,
        BusinessMetrics metrics)
    {
        _assignmentRepository = assignmentRepository;
        _eventRepository = eventRepository;
        _authorityCeilingService = authorityCeilingService;
        _metrics = metrics;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(RevokeEventRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetById(request.AssignmentId);
        if (assignment is null || assignment.TenantId != request.TenantId || assignment.EventId != request.EventId)
        {
            return Failure("Event role assignment not found for the requested event.", "event_role_assignment_not_found", request.AssignmentId);
        }

        var @event = await GetEventInTenantAsync(_eventRepository, request.TenantId, request.EventId);
        if (@event is null)
        {
            return Failure("Event not found for the requested tenant.", "event_not_found");
        }

        if (assignment.RoleId == (int)RoleEnum.EventOwner)
        {
            return Failure(
                "EventOwner assignments cannot be revoked directly. Transfer ownership first.",
                "event_owner_transfer_required",
                assignment.Id);
        }

        var authority = await _authorityCeilingService.CanAssignRoleAsync(
            request.TenantId, request.EventId, request.ActorUserId, assignment.RoleId, cancellationToken);

        if (!authority.IsAllowed)
        {
            _metrics.RecordEventRoleAssignmentChanged("revoke", "denied", RoleCodeFor(assignment.RoleId));
            return AuthorityFailure(authority);
        }

        assignment.Revoke(request.ActorUserId, DateTime.UtcNow);
        await _assignmentRepository.Update(assignment);
        _metrics.RecordEventRoleAssignmentChanged("revoke", "allowed", RoleCodeFor(assignment.RoleId));

        return Success(assignment.Id, "Event role assignment revoked successfully.");
    }
}
