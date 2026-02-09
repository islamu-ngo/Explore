using System.Collections.Generic;
using Explore.Application.DTOs.EventTags;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Queries;

public class GetEventTagsListRequest : IRequest<List<EventTagsListDto>>
{
}
