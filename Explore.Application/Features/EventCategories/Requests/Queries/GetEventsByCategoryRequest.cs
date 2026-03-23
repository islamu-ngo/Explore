// ABOUTME: MediatR query for fetching events in a given category.
// ABOUTME: Returns IEnumerable<EventListDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Queries;

public class GetEventsByCategoryRequest : IRequest<List<EventListDto>>
{
    public Guid CategoryId { get; set; }
}
