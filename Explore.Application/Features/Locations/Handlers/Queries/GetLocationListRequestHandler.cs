using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Locations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Locations.Handlers.Queries
{
    public class GetLocationListRequestHandler : IRequestHandler<GetLocationListRequest, List<LocationListDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;

        public GetLocationListRequestHandler(
            ILocationRepository locationRepository,
            IMapper mapper)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
        }

        public async Task<List<LocationListDto>> Handle(GetLocationListRequest request, CancellationToken cancellationToken)
        {
            var locations = await _locationRepository.GetAll();
            return _mapper.Map<List<LocationListDto>>(locations);
        }
    }
}
