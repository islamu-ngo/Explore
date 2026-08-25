// ABOUTME: Handler for grouped Location PATCH updates with optimistic concurrency.
// ABOUTME: Validates groups, loads Location once, applies explicit field updates, and saves once.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.Location.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.ValueObjects;
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
        var validator = new UpdateLocationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.UpdateLocationDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BaseCommandResponse.Validation<Guid>(
                validationResult.Errors.Select(e => e.ErrorMessage),
                "Location update failed.");
        }

        var location = await _locationRepository.GetById(request.LocationId);

        if (location == null)
        {
            return BaseCommandResponse.NotFound<Guid>("Location not found.");
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
        ApplyCountry(location, request.UpdateLocationDto.Country);
        ApplyCity(location, request.UpdateLocationDto.City);
        ApplyCoordinates(location, request.UpdateLocationDto.Latitude, request.UpdateLocationDto.Longitude);
        ApplyManualAddress(
            location,
            request.UpdateLocationDto.Address,
            request.UpdateLocationDto.Postcode);
        ApplyTimezone(location, request.UpdateLocationDto.Timezone);

        await _locationRepository.Update(location);

        return BaseCommandResponse.Success(location.Id, "Location updated successfully.");
    }

    private static void ApplyFullName(Location location, UpdateLocationFullNameDto? group)
    {
        if (group is not null)
        {
            location.FullName = group.Value;
        }
    }

    private static void ApplyManualAddress(
        Location location,
        UpdateLocationAddressDto? address,
        UpdateLocationPostcodeDto? postcode)
    {
        if (address is null && postcode is null)
        {
            return;
        }

        location.SetManualAddress(
            address?.Value ?? location.Address!,
            postcode?.Value ?? location.Postcode!);
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

    private static void ApplyCoordinates(
        Location location,
        UpdateLocationLatitudeDto? latitude,
        UpdateLocationLongitudeDto? longitude)
    {
        if (latitude is null && longitude is null)
        {
            return;
        }

        double? latitudeValue = latitude!.Value.Value;
        double? longitudeValue = longitude!.Value.Value;
        GeoCoordinate? coordinate = latitudeValue.HasValue
            ? GeoCoordinate.Create(latitudeValue.Value, longitudeValue!.Value)
            : null;
        location.SetProviderAddress(location.Address!, location.Postcode!, coordinate);
    }

    private static void ApplyTimezone(Location location, UpdateLocationTimezoneDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            location.Timezone = group.Value.Value;
        }
    }
}
