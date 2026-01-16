using Explore.Application.DTOs.EventTags;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.EventTags.Requests.Commands
{
    public class CreateEventTagsCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateEventTagsDto EventTagsDto { get; set; }
    }
}
