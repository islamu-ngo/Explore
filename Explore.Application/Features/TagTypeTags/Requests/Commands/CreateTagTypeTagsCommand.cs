using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Commands
{
    public class CreateTagTypeTagsCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateTagTypeTagsDto TagTypeTagsDto { get; set; }
    }
}
