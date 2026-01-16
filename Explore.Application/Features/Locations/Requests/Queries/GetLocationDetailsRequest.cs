using System;
using Explore.Application.DTOs.Location;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Queries
{
    public class GetLocationDetailsRequest : IRequest<LocationDto>
    {
        public Guid Id { get; set; }
    }
}
