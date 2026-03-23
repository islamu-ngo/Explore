// ABOUTME: MediatR query request for fetching a single session by ID.
// ABOUTME: Returns EventSessionDto.
using System;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

public class GetEventSessionDetailsRequest : IRequest<EventSessionDto>
{
    public Guid Id { get; set; }
}
