using System;
using Explore.Application.DTOs.Tag;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Commands
{
    public class CreateTagCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateTagDto TagDto { get; set; }
    }
}
