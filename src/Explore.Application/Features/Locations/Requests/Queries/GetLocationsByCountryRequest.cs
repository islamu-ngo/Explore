// ABOUTME: MediatR query for fetching locations in a given country.
// ABOUTME: Returns IEnumerable<LocationDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.Location;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Queries;

public class GetLocationsByCountryRequest : IRequest<List<LocationListDto>>
{
    public required string Country { get; set; }
}
