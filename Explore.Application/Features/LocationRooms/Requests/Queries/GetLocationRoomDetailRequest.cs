// ABOUTME: MediatR query for retrieving a single room by Id.
// ABOUTME: Returns null when not found; caller translates to 404.

using Explore.Application.DTOs.LocationRoom;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Queries;

public class GetLocationRoomDetailRequest : IRequest<LocationRoomDto?>
{
    public Guid Id { get; set; }
}
