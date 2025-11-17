using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries
{
    public class GetEventListRequest : IRequest<List<EventListDto>>
    {
        public Guid Id { get; set; }
    }
}
