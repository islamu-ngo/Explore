// ABOUTME: MediatR query for retrieving a single EventDay by Id.
// ABOUTME: Returns null when not found; caller translates to 404.

using Explore.Application.DTOs.EventDay;
using MediatR;

namespace Explore.Application.Features.EventDays.Requests.Queries;

public sealed record GetEventDayDetailRequest(Guid Id) : IRequest<EventDayDto?>;
