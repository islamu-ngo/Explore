// ABOUTME: Handler for grouped Location PATCH updates with optimistic concurrency.
// ABOUTME: Validates groups, loads Location once, applies explicit field updates, and saves once.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.Location.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Commands;

public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, BaseCommandResponse<Guid>>
{
    private readonly ILocationRepository _locationRepository;

    public UpdateLocationCommandHandler(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateLocationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.UpdateLocationDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Location update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var location = await _locationRepository.GetById(request.LocationId);

        if (location == null)
        {
            response.Success = false;
            response.Message = "Location not found.";
            return response;
        }

        if (location.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The location was modified by another request. Reload and retry.",
                nameof(Location),
                location.Id.ToString());
        }

        ApplyFullName(location, request.UpdateLocationDto.FullName);
        ApplyAddress(location, request.UpdateLocationDto.Address);
        ApplyPostcode(location, request.UpdateLocationDto.Postcode);
        ApplyCountry(location, request.UpdateLocationDto.Country);
        ApplyCity(location, request.UpdateLocationDto.City);
        ApplyLatitude(location, request.UpdateLocationDto.Latitude);
        ApplyLongitude(location, request.UpdateLocationDto.Longitude);
        ApplyTimezone(location, request.UpdateLocationDto.Timezone);

        await _locationRepository.Update(location);

        response.Success = true;
        response.Id = location.Id;
        response.Message = "Location updated successfully.";

        return response;
    }

    private static void ApplyFullName(Location location, UpdateLocationFullNameDto? group)
    {
        if (group is not null)
        {
            location.FullName = group.Value;
        }
    }

    private static void ApplyAddress(Location location, UpdateLocationAddressDto? group)
    {
        if (group is not null)
        {
            location.Address = group.Value;
        }
    }

    private static void ApplyPostcode(Location location, UpdateLocationPostcodeDto? group)
    {
        if (group is not null)
        {
            location.Postcode = group.Value;
        }
    }

    private static void ApplyCountry(Location location, UpdateLocationCountryDto? group)
    {
        if (group is not null)
        {
            location.Country = group.Value;
        }
    }

    private static void ApplyCity(Location location, UpdateLocationCityDto? group)
    {
        if (group is not null)
        {
            location.City = group.Value;
        }
    }

    private static void ApplyLatitude(Location location, UpdateLocationLatitudeDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            location.Latitude = group.Value.Value;
        }
    }

    private static void ApplyLongitude(Location location, UpdateLocationLongitudeDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            location.Longitude = group.Value.Value;
        }
    }

    private static void ApplyTimezone(Location location, UpdateLocationTimezoneDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            location.Timezone = group.Value.Value;
        }
    }
}
