using System;
using Explore.Application.DTOs.Location;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands;

public class UpdateLocationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateLocationDto LocationDto { get; set; }
}
