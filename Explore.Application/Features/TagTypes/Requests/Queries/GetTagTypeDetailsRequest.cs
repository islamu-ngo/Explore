using Explore.Application.DTOs.TagType;
using MediatR;

namespace Explore.Application.Features.TagTypes.Requests.Queries
{
    public class GetTagTypeDetailsRequest : IRequest<TagTypeDto>
    {
        public int Id { get; set; }
    }
}
