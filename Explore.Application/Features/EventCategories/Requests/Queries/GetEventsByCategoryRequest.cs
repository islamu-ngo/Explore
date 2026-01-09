using Explore.Application.DTOs.Event;
using MediatR;
using System;
using System.Collections.Generic;

namespace Explore.Application.Features.EventCategories.Requests.Queries
{
    public class GetEventsByCategoryRequest : IRequest<List<EventListDto>>
    {
        public Guid CategoryId { get; set; }
    }
}
