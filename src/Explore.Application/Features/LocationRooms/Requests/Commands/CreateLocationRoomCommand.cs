// ABOUTME: MediatR command for creating a new room under a location.
// ABOUTME: Secured via AuthorizeResource for the location_room resource kind.

using Explore.Application.Authorization;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Commands;

[AuthorizeResource(ResourceKinds.LocationRoom, AuthorizationActions.Create)]
public sealed record CreateLocationRoomCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateLocationRoomDto LocationRoomDto { get; init; }

    string? ISecureRequest.ResourceId => null;
}
