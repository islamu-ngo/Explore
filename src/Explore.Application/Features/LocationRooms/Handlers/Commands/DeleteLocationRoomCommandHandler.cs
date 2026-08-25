// ABOUTME: Handler for soft-deleting a room by Id.
// ABOUTME: Follows the pattern where delete returns BaseCommandResponse<Guid>.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.LocationRooms.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Handlers.Commands;

public class DeleteLocationRoomCommandHandler : IRequestHandler<DeleteLocationRoomCommand, BaseCommandResponse<Guid>>
{
    private readonly ILocationRoomRepository _locationRoomRepository;

    public DeleteLocationRoomCommandHandler(ILocationRoomRepository locationRoomRepository)
    {
        _locationRoomRepository = locationRoomRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeleteLocationRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _locationRoomRepository.GetById(request.Id);
        if (room == null)
        {
            return BaseCommandResponse.Validation<Guid>(["Room not found."], "Room not found.");
        }

        await _locationRoomRepository.Delete(room);

        return BaseCommandResponse.Success(room.Id, "Room deleted successfully.");
    }
}
