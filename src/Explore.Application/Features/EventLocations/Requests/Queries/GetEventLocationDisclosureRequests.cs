// ABOUTME: Purpose-specific EventLocation read requests for public, attendee, and management API surfaces.
// ABOUTME: Keeps requester identity server-derived and scopes every disclosure read to its parent event.

using Explore.Application.DTOs.Location;
using MediatR;

namespace Explore.Application.Features.EventLocations.Requests.Queries;

public sealed record GetPublicEventLocationsRequest(Guid EventId)
    : IRequest<IReadOnlyList<EventLocationPublicDto>?>;

public sealed record GetAttendeeEventLocationsRequest(Guid EventId)
    : IRequest<IReadOnlyList<EventLocationAttendeeDto>?>;

public sealed record GetManagementEventLocationRequest(Guid EventId, Guid EventLocationId)
    : IRequest<EventLocationManagementDto?>;
