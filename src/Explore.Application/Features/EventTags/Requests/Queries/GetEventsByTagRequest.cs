// ABOUTME: MediatR query for fetching events with a given tag.
// ABOUTME: Returns IEnumerable<EventListDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Queries;

public class GetEventsByTagRequest : IRequest<List<EventListDto>>
{
    public Guid TagId { get; set; }
}
