// ABOUTME: Query for event-scoped program sections/tracks/devrooms.
// ABOUTME: Keeps program grouping reads in Application rather than composing EF queries in API.

using Explore.Application.DTOs.EventSessionGroup;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Queries;

public sealed record GetEventSessionGroupsByEventRequest(Guid EventId = default)
    : IRequest<List<EventSessionGroupListDto>>;
