// ABOUTME: Query request for sessions assigned to a program section, track, devroom, or stage.
// ABOUTME: Keeps group membership reads behind CQRS instead of exposing repository details to API controllers.

using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Queries;

public class GetEventSessionGroupSessionsRequest : IRequest<List<EventSessionListDto>>
{
    public Guid EventSessionGroupId { get; set; }
}
