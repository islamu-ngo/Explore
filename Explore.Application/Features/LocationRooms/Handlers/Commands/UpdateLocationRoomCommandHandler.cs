// ABOUTME: Handler for updating an existing room with validation.
// ABOUTME: Validates location ownership, applies field updates via AutoMapper.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.LocationRoom.Validators;
using Explore.Application.Features.LocationRooms.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Handlers.Commands;

public class UpdateLocationRoomCommandHandler : IRequestHandler<UpdateLocationRoomCommand, BaseCommandResponse<Guid>>
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public UpdateLocationRoomCommandHandler(
        ILocationRoomRepository locationRoomRepository,
        ILocationRepository locationRepository,
        IMapper mapper)
    {
        _locationRoomRepository = locationRoomRepository;
        _locationRepository = locationRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateLocationRoomCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateLocationRoomDtoValidator(_locationRepository);
        var validationResult = await validator.ValidateAsync(request.LocationRoomDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Room update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var room = await _locationRoomRepository.GetById(request.LocationRoomDto.Id);
        if (room == null)
        {
            response.Success = false;
            response.Message = "Room not found.";
            return response;
        }

        var parentLocation = await _locationRepository.GetById(request.LocationRoomDto.LocationId);
        if (parentLocation == null || parentLocation.TenantId != room.TenantId)
        {
            response.Success = false;
            response.Message = "Location does not belong to the same tenant as the room.";
            return response;
        }

        _mapper.Map(request.LocationRoomDto, room);

        await _locationRoomRepository.Update(room);

        response.Success = true;
        response.Id = room.Id;
        response.Message = "Room updated successfully.";

        return response;
    }
}
