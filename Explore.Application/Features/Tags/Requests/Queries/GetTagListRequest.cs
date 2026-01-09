using System.Collections.Generic;
using Explore.Application.DTOs.Tag;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Queries
{
    public class GetTagListRequest : IRequest<List<TagListDto>>
    {
    }
}
