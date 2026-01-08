using System;
using Explore.Application.DTOs.Location;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands
{
    public class CreateLocationCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateLocationDto LocationDto { get; set; }
    }
}
