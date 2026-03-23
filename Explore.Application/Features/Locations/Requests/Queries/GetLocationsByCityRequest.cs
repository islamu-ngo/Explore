// ABOUTME: MediatR query for fetching locations in a given city.
// ABOUTME: Returns IEnumerable<LocationDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.Location;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Queries;

public class GetLocationsByCityRequest : IRequest<List<LocationListDto>>
{
    public required string City { get; set; }
}
