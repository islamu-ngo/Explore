using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries
{
    public class GetMyEventsRequest : IRequest<List<EventListDto>>
    {
        public string UserId { get; set; }
    }
}
