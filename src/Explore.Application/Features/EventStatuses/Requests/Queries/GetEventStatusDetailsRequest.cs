// ABOUTME: MediatR query request for fetching a single event status by ID.
// ABOUTME: Returns EventStatusDto.
using Explore.Application.DTOs.EventStatus;
using MediatR;

namespace Explore.Application.Features.EventStatuses.Requests.Queries;

public sealed record GetEventStatusDetailsRequest(int Id = default) : IRequest<EventStatusDto>;
