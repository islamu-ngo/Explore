// ABOUTME: MediatR query for fetching all sessions in an event.
// ABOUTME: Returns IEnumerable<EventSessionDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

public class GetSessionsByEventRequest : IRequest<List<EventSessionListDto>>
{
    public Guid EventId { get; set; }
}
