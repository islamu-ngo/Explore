using System.Collections.Generic;
using Explore.Application.DTOs.Location;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Queries
{
    public class GetLocationsByCountryRequest : IRequest<List<LocationListDto>>
    {
        public string Country { get; set; }
    }
}
