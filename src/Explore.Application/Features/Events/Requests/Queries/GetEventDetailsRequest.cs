// ABOUTME: MediatR query request for fetching full event details by ID or slug.
// ABOUTME: Returns EventDto.
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record GetEventDetailsRequest(Guid Id = default) : IRequest<EventDto>;
