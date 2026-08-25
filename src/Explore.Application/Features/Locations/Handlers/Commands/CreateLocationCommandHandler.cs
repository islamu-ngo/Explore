// ABOUTME: Handler for creating a new event location with validation.
// ABOUTME: Validates input, maps DTO, sets TenantId, persists via repository.
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
using Explore.Domain.ValueObjects;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Commands;

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
        var validator = new CreateLocationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.LocationDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BaseCommandResponse.Validation<Guid>(
                validationResult.Errors.Select(e => e.ErrorMessage),
                "Location creation failed.");
        }
        var location = _mapper.Map<Location>(request.LocationDto);
        GeoCoordinate? coordinate = request.LocationDto.Latitude.HasValue
            ? GeoCoordinate.Create(
                request.LocationDto.Latitude.Value,
                request.LocationDto.Longitude!.Value)
            : null;
        location.SetProviderAddress(
            request.LocationDto.Address,
            request.LocationDto.Postcode,
            coordinate);

        // Set TenantId from the request context
        location.TenantId = _tenantContext.TenantId;

        location = await _locationRepository.Create(location);

        return BaseCommandResponse.Success(location.Id, "Location created successfully.");
    }
}
