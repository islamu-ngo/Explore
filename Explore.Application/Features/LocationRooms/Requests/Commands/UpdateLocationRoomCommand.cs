// ABOUTME: MediatR command for PATCH-based LocationRoom updates.
// ABOUTME: Carries route authority, If-Match concurrency, and grouped room update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Commands;

[AuthorizeResource(ResourceKinds.LocationRoom, AuthorizationActions.Update)]
public class UpdateLocationRoomCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid LocationRoomId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateLocationRoomDto UpdateLocationRoomDto { get; set; }

    string? ISecureRequest.ResourceId => LocationRoomId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
