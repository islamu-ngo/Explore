// ABOUTME: Handler for soft-deleting event session groups while preserving EventSession program items.
// ABOUTME: Uses repository soft-delete semantics for explicit group removal.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Commands;

public class DeleteEventSessionGroupCommandHandler : IRequestHandler<DeleteEventSessionGroupCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;
    private readonly IEventSessionGroupSessionRepository _eventSessionGroupSessionRepository;

    public DeleteEventSessionGroupCommandHandler(
        IEventSessionGroupRepository eventSessionGroupRepository,
        IEventSessionGroupSessionRepository eventSessionGroupSessionRepository)
    {
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _eventSessionGroupSessionRepository = eventSessionGroupSessionRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeleteEventSessionGroupCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var group = await _eventSessionGroupRepository.GetForUpdateAsync(request.Id, cancellationToken);
        if (group is null)
        {
            response.Success = false;
            response.Message = "Event session group not found.";
            return response;
        }

        if (group.EventId != request.EventId)
        {
            response.Success = false;
            response.Message = "Event session group must belong to the requested event.";
            return response;
        }

        var assignments = await _eventSessionGroupSessionRepository.GetAssignmentsForGroupUpdateAsync(
            group.Id,
            cancellationToken);
        foreach (var assignment in assignments)
        {
            await _eventSessionGroupSessionRepository.Delete(assignment);
        }

        await _eventSessionGroupRepository.Delete(group);

        response.Success = true;
        response.Id = group.Id;
        response.Message = "Event session group deleted successfully.";
        return response;
    }
}
