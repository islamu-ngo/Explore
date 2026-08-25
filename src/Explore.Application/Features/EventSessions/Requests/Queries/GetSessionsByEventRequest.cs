// ABOUTME: MediatR query for fetching all sessions in an event.
// ABOUTME: Returns IEnumerable<EventSessionDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

public sealed record GetSessionsByEventRequest(Guid EventId = default) : IRequest<List<EventSessionListDto>>;
