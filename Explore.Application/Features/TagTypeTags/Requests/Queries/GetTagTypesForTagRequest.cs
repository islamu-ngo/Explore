using Explore.Application.DTOs.TagType;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Queries
{
    public class GetTagTypesForTagRequest : IRequest<List<TagTypeListDto>>
    {
        public Guid TagId { get; set; }
    }
}
