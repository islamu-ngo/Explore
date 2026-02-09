using System.Collections.Generic;
using Explore.Application.DTOs.EventStatus;
using MediatR;

namespace Explore.Application.Features.EventStatuses.Requests.Queries;

public class GetEventStatusListRequest : IRequest<List<EventStatusListDto>>
{
}
