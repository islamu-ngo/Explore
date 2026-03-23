// ABOUTME: Query handler returning all locations in a given city.
// ABOUTME: Filters by city name, maps to LocationDto list.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Locations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Queries;

public class GetLocationsByCityRequestHandler : IRequestHandler<GetLocationsByCityRequest, List<LocationListDto>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public GetLocationsByCityRequestHandler(
        ILocationRepository locationRepository,
        IMapper mapper)
    {
        _locationRepository = locationRepository;
        _mapper = mapper;
    }

    public async Task<List<LocationListDto>> Handle(GetLocationsByCityRequest request, CancellationToken cancellationToken)
    {
        var locations = await _locationRepository.GetLocationsByCity(request.City);
        return _mapper.Map<List<LocationListDto>>(locations);
    }
}
