// ABOUTME: MediatR command for updating an existing room.
// ABOUTME: Secured via AuthorizeResource for the location_room resource kind.

using Explore.Application.Authorization;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Commands;

[AuthorizeResource(ResourceKinds.LocationRoom, AuthorizationActions.Update)]
public class UpdateLocationRoomCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateLocationRoomDto LocationRoomDto { get; set; }

    string? ISecureRequest.ResourceId => LocationRoomDto.Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
