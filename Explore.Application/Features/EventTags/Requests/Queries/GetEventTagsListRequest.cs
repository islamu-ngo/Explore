using Explore.Application.DTOs.EventTags;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.EventTags.Requests.Queries
{
    public class GetEventTagsListRequest : IRequest<List<EventTagsListDto>>
    {
    }
}
