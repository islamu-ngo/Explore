using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location.Validators;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Commands
{
    public class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, BaseCommandResponse<Guid>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;

        public CreateLocationCommandHandler(
            ILocationRepository locationRepository,
            ITenantContext tenantContext,
            IMapper mapper)
        {
            _locationRepository = locationRepository;
            _tenantContext = tenantContext;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateLocationDtoValidator();
            var validationResult = await validator.ValidateAsync(request.LocationDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Location creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var location = _mapper.Map<Location>(request.LocationDto);

            // Set TenantId from the request context
            location.TenantId = _tenantContext.TenantId;

            location = await _locationRepository.Create(location);

            response.Success = true;
            response.Id = location.Id;
            response.Message = "Location created successfully.";

            return response;
        }
    }
}
