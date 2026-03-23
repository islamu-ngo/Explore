// ABOUTME: Query handler returning all locations in a given country.
// ABOUTME: Filters by country, maps to LocationDto list.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Locations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Queries;

public class GetLocationsByCountryRequestHandler : IRequestHandler<GetLocationsByCountryRequest, List<LocationListDto>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public GetLocationsByCountryRequestHandler(
        ILocationRepository locationRepository,
        IMapper mapper)
    {
        _locationRepository = locationRepository;
        _mapper = mapper;
    }

    public async Task<List<LocationListDto>> Handle(GetLocationsByCountryRequest request, CancellationToken cancellationToken)
    {
        var locations = await _locationRepository.GetLocationsByCountry(request.Country);
        return _mapper.Map<List<LocationListDto>>(locations);
    }
}
