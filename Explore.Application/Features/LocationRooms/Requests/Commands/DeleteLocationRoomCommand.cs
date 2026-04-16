// ABOUTME: MediatR command for soft-deleting a room.
// ABOUTME: Secured via AuthorizeResource for the location_room resource kind.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Commands;

[AuthorizeResource(ResourceKinds.LocationRoom, AuthorizationActions.Delete)]
public class DeleteLocationRoomCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
