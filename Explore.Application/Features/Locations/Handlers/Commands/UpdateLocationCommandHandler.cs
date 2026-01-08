using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location.Validators;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Commands
{
    public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, BaseCommandResponse<Guid>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;

        public UpdateLocationCommandHandler(
            ILocationRepository locationRepository,
            IMapper mapper)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new UpdateLocationDtoValidator();
            var validationResult = await validator.ValidateAsync(request.LocationDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Location update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var location = await _locationRepository.GetById(request.LocationDto.Id);

            if (location == null)
            {
                response.Success = false;
                response.Message = "Location not found.";
                return response;
            }

            _mapper.Map(request.LocationDto, location);

            await _locationRepository.Update(location);

            response.Success = true;
            response.Id = location.Id;
            response.Message = "Location updated successfully.";

            return response;
        }
    }
}
