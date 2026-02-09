using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Queries;

public class GetEventsByCategoryRequest : IRequest<List<EventListDto>>
{
    public Guid CategoryId { get; set; }
}
