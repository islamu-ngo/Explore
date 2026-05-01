// ABOUTME: Handler for updating event-role validity windows.
// ABOUTME: Reuses authority ceiling and domain lifecycle validation.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventRoleAssignments.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using MediatR;

namespace Explore.Application.Features.EventRoleAssignments.Handlers.Commands;

public sealed class UpdateEventRoleAssignmentWindowCommandHandler
    : EventRoleAssignmentCommandHandlerBase,
      IRequestHandler<UpdateEventRoleAssignmentWindowCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRoleAssignmentRepository _assignmentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventRoleAuthorityCeilingService _authorityCeilingService;
    private readonly BusinessMetrics _metrics;

    public UpdateEventRoleAssignmentWindowCommandHandler(
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

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventRoleAssignmentWindowCommand request, CancellationToken cancellationToken)
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

        var authority = await _authorityCeilingService.CanAssignRoleAsync(
            request.TenantId, request.EventId, request.ActorUserId, assignment.RoleId, cancellationToken);

        if (!authority.IsAllowed)
        {
            _metrics.RecordEventRoleAssignmentChanged("update-window", "denied", RoleCodeFor(assignment.RoleId));
            return AuthorityFailure(authority);
        }

        assignment.UpdateValidityWindow(request.StartsAtUtc, request.ExpiresAtUtc, DateTime.UtcNow);
        await _assignmentRepository.Update(assignment);
        _metrics.RecordEventRoleAssignmentChanged("update-window", "allowed", RoleCodeFor(assignment.RoleId));

        return Success(assignment.Id, "Event role assignment updated successfully.");
    }
}
