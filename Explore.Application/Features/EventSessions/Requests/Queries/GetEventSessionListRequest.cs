using System.Collections.Generic;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries
{
    public class GetEventSessionListRequest : IRequest<List<EventSessionListDto>>
    {
    }
}
