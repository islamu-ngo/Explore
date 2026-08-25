// ABOUTME: MediatR query for retrieving a single event series by ID.
// ABOUTME: Returns EventSeriesDto with associated events, or null if not found.

using Explore.Application.DTOs.EventSeries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Queries;

public sealed record GetEventSeriesDetailRequest(Guid Id = default) : IRequest<EventSeriesDto?>;
