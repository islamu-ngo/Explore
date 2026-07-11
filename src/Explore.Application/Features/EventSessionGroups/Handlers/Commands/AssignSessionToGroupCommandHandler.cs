// ABOUTME: Handler for creating or updating a session-to-group assignment.
// ABOUTME: Enforces same-event membership and ensures only one primary group per session.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionGroup.Validators;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Commands;

public class AssignSessionToGroupCommandHandler : IRequestHandler<AssignSessionToGroupCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;
    private readonly IEventSessionGroupSessionRepository _eventSessionGroupSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventSessionRepository _eventSessionRepository;

    public AssignSessionToGroupCommandHandler(
        IEventSessionGroupRepository eventSessionGroupRepository,
        IEventSessionGroupSessionRepository eventSessionGroupSessionRepository,
        IEventRepository eventRepository,
        IEventSessionRepository eventSessionRepository)
    {
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _eventSessionGroupSessionRepository = eventSessionGroupSessionRepository;
        _eventRepository = eventRepository;
        _eventSessionRepository = eventSessionRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(AssignSessionToGroupCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new AssignSessionToGroupRequestDtoValidator(
            _eventRepository,
            _eventSessionGroupRepository,
            _eventSessionRepository);
        var validationResult = await validator.ValidateAsync(request.Assignment, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Session group assignment failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var group = await _eventSessionGroupRepository.GetForUpdateAsync(
            request.Assignment.EventSessionGroupId,
            cancellationToken);
        var session = await _eventSessionRepository.GetById(request.Assignment.EventSessionId);
        var parentEvent = await _eventRepository.GetById(request.Assignment.EventId);

        if (group is null || session is null || parentEvent is null)
        {
            response.Success = false;
            response.Message = "Event, session group, or event session was not found in the current tenant.";
            return response;
        }

        if (group.EventId != request.Assignment.EventId || session.EventId != request.Assignment.EventId)
        {
            response.Success = false;
            response.Message = "Session group and event session must belong to the requested event.";
            return response;
        }

        var existingAssignment = await _eventSessionGroupSessionRepository.GetExistingAssignmentAsync(
            request.Assignment.EventSessionGroupId,
            request.Assignment.EventSessionId,
            cancellationToken);

        if (request.Assignment.IsPrimary)
        {
            await DemoteOtherPrimaryAssignmentsAsync(session.Id, existingAssignment?.Id, cancellationToken);
        }

        if (existingAssignment is not null)
        {
            existingAssignment.EventId = request.Assignment.EventId;
            existingAssignment.IsPrimary = request.Assignment.IsPrimary;
            existingAssignment.SortOrder = request.Assignment.SortOrder;
            await _eventSessionGroupSessionRepository.Update(existingAssignment);

            response.Success = true;
            response.Id = existingAssignment.Id;
            response.Message = "Session group assignment updated successfully.";
            return response;
        }

        var assignment = new EventSessionGroupSession
        {
            EventSessionGroupId = group.Id,
            EventSessionGroup = null!,
            EventSessionId = session.Id,
            EventSession = null!,
            EventId = parentEvent.Id,
            Event = null!,
            TenantId = parentEvent.TenantId,
            Tenant = null!,
            IsPrimary = request.Assignment.IsPrimary,
            SortOrder = request.Assignment.SortOrder
        };

        assignment = await _eventSessionGroupSessionRepository.Create(assignment);

        response.Success = true;
        response.Id = assignment.Id;
        response.Message = "Session group assignment created successfully.";
        return response;
    }

    private async Task DemoteOtherPrimaryAssignmentsAsync(
        Guid eventSessionId,
        Guid? excludeAssignmentId,
        CancellationToken cancellationToken)
    {
        var primaryAssignments = await _eventSessionGroupSessionRepository.GetPrimaryAssignmentsForSessionAsync(
            eventSessionId,
            excludeAssignmentId,
            cancellationToken);

        foreach (var assignment in primaryAssignments)
        {
            assignment.IsPrimary = false;
            await _eventSessionGroupSessionRepository.Update(assignment);
        }
    }
}
