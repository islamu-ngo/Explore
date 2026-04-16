// ABOUTME: Handler for creating a new room under a location with validation.
// ABOUTME: Validates location ownership, maps DTO, sets TenantId from parent location.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.LocationRoom.Validators;
using Explore.Application.Features.LocationRooms.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Handlers.Commands;

public class CreateLocationRoomCommandHandler : IRequestHandler<CreateLocationRoomCommand, BaseCommandResponse<Guid>>
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public CreateLocationRoomCommandHandler(
        ILocationRoomRepository locationRoomRepository,
        ILocationRepository locationRepository,
        IMapper mapper)
    {
        _locationRoomRepository = locationRoomRepository;
        _locationRepository = locationRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateLocationRoomCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateLocationRoomDtoValidator(_locationRepository);
        var validationResult = await validator.ValidateAsync(request.LocationRoomDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Room creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var parentLocation = await _locationRepository.GetById(request.LocationRoomDto.LocationId);
        if (parentLocation == null)
        {
            response.Success = false;
            response.Message = "Location not found in the current tenant.";
            return response;
        }

        var room = _mapper.Map<LocationRoom>(request.LocationRoomDto);
        room.TenantId = parentLocation.TenantId;

        room = await _locationRoomRepository.Create(room);

        response.Success = true;
        response.Id = room.Id;
        response.Message = "Room created successfully.";

        return response;
    }
}
