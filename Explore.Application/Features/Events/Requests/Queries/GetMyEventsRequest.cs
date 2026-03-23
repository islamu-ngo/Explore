// ABOUTME: MediatR query request for fetching the current user's events.
// ABOUTME: Returns IEnumerable<EventListDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public class GetMyEventsRequest : IRequest<PaginatedResult<EventListDto>>
{
    public required string UserId { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
}
