// ABOUTME: MediatR command for PATCH-based LocationRoom updates.
// ABOUTME: Carries route authority, If-Match concurrency, and grouped room update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Commands;

[AuthorizeResource(ResourceKinds.LocationRoom, AuthorizationActions.Update)]
public sealed record UpdateLocationRoomCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid LocationRoomId { get; init; }

    public Guid ExpectedConcurrencyStamp { get; init; }

    public required UpdateLocationRoomDto UpdateLocationRoomDto { get; init; }

    string? ISecureRequest.ResourceId => LocationRoomId.ToString();
}
