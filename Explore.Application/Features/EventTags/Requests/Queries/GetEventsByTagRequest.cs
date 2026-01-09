using Explore.Application.DTOs.Event;
using MediatR;
using System;
using System.Collections.Generic;

namespace Explore.Application.Features.EventTags.Requests.Queries
{
    public class GetEventsByTagRequest : IRequest<List<EventListDto>>
    {
        public Guid TagId { get; set; }
    }
}
