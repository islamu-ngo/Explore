// ABOUTME: Query for a single event session group detail read model.
// ABOUTME: Supports HATEOAS detail endpoints and future group edit screens.

using Explore.Application.DTOs.EventSessionGroup;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Queries;

public class GetEventSessionGroupDetailRequest : IRequest<EventSessionGroupDto?>
{
    public Guid Id { get; set; }
}
