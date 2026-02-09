using Explore.Application.DTOs.TagTypeTags;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Queries;

public class GetTagTypeTagsDetailsRequest : IRequest<TagTypeTagsDto>
{
    public Guid Id { get; set; }
}
