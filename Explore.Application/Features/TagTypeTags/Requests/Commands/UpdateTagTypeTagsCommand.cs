using Explore.Application.DTOs.TagTypeTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Commands;

public class UpdateTagTypeTagsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateTagTypeTagsDto TagTypeTagsDto { get; set; }
}
