// ABOUTME: MediatR query for retrieving all rooms belonging to a specific location.
// ABOUTME: Returns a list since rooms per location are typically small (< 50).

using Explore.Application.DTOs.LocationRoom;
using MediatR;

namespace Explore.Application.Features.LocationRooms.Requests.Queries;

public class GetLocationRoomsByLocationRequest : IRequest<List<LocationRoomListDto>>
{
    public Guid LocationId { get; set; }
}
