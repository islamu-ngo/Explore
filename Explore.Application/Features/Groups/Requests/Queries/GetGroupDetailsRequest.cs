// ABOUTME: MediatR query request for retrieving a single Group with full details.
// ABOUTME: Returns a GroupDto mapped from the Group entity with navigation properties.

using Explore.Application.DTOs.Group;
using MediatR;

namespace Explore.Application.Features.Groups.Requests.Queries;

public class GetGroupDetailsRequest : IRequest<GroupDto>
{
    public Guid Id { get; set; }
}
