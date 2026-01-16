using System;
using Explore.Application.DTOs.Tag;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Commands
{
    public class UpdateTagCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateTagDto TagDto { get; set; }
    }
}
