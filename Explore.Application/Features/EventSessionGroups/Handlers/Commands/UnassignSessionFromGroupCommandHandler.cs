// ABOUTME: Handler for soft-removing a session/group assignment.
// ABOUTME: Deletes only the EventSessionGroupSession join row, never the underlying session.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Commands;

public class UnassignSessionFromGroupCommandHandler : IRequestHandler<UnassignSessionFromGroupCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionGroupSessionRepository _eventSessionGroupSessionRepository;

    public UnassignSessionFromGroupCommandHandler(IEventSessionGroupSessionRepository eventSessionGroupSessionRepository)
    {
        _eventSessionGroupSessionRepository = eventSessionGroupSessionRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UnassignSessionFromGroupCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var assignment = await _eventSessionGroupSessionRepository.GetExistingAssignmentAsync(
            request.EventSessionGroupId,
            request.EventSessionId,
            cancellationToken);

        if (assignment is null)
        {
            response.Success = false;
            response.Message = "Session group assignment not found.";
            return response;
        }

        if (assignment.EventId != request.EventId)
        {
            response.Success = false;
            response.Message = "Session group assignment must belong to the requested event.";
            return response;
        }

        await _eventSessionGroupSessionRepository.Delete(assignment);

        response.Success = true;
        response.Id = assignment.Id;
        response.Message = "Session group assignment removed successfully.";
        return response;
    }
}
