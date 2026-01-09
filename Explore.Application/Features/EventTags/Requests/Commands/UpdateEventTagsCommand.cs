using Explore.Application.DTOs.EventTags;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.EventTags.Requests.Commands
{
    public class UpdateEventTagsCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateEventTagsDto EventTagsDto { get; set; }
    }
}
