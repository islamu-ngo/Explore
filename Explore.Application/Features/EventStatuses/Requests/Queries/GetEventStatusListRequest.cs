using Explore.Application.DTOs.EventStatus;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.EventStatuses.Requests.Queries
{
    public class GetEventStatusListRequest : IRequest<List<EventStatusListDto>>
    {
    }
}
