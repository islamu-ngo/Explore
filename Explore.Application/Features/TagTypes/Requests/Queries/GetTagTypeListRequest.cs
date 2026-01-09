using Explore.Application.DTOs.TagType;
using MediatR;

namespace Explore.Application.Features.TagTypes.Requests.Queries
{
    public class GetTagTypeListRequest : IRequest<List<TagTypeListDto>>
    {
    }
}
